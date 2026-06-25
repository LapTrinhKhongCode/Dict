#!/usr/bin/env python3
"""
optuna_sweep.py — Tunes RagTuning params by writing to appsettings.Development.json,
scoring on dev_set, and tracking best objective across trials.

HOW IT WORKS:
  Optuna uses TPE (Tree-structured Parzen Estimator):
  - Trials 1-10: random sampling to explore the param space
  - Trial 11+:   builds two distributions l(x) / g(x) from past good/bad trials
                 and proposes params from high l(x)/g(x) ratio (Bayesian optimization)
  - No gradients needed — just needs a scalar objective per trial

WORKFLOW (C# backend — no hot-reload):
  1. Optuna proposes a param set
  2. Script writes params to appsettings.Development.json
  3. You rebuild + restart backend manually (or use --auto-restart if dotnet watch)
  4. Script calls API for all dev_set questions, computes objective
  5. Repeat from step 1

Usage:
  # Validate script (no backend restart, subset of 10 questions):
  python optuna_sweep.py --trials 1 --dry-run --max-questions 10

  # Suggest next param set to try (write to appsettings, print what to change):
  python optuna_sweep.py --suggest-only

  # Full sweep (requires backend restartable via dotnet run):
  python optuna_sweep.py --trials 20 --max-questions 30

  # Resume previous study:
  python optuna_sweep.py --trials 20 --storage sqlite:///sweep.db

Objective (maximize):
  0.4 * page_hit_rate + 0.3 * point_recall + 0.2 * refuse_accuracy - latency_penalty
"""

import argparse
import json
import re
import subprocess
import time
import unicodedata
from pathlib import Path

import optuna
import requests
import urllib3
from dotenv import load_dotenv
import os

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
optuna.logging.set_verbosity(optuna.logging.WARNING)

# ── Config ────────────────────────────────────────────────────────────────────
EVAL_DIR       = Path(__file__).parent
DATASETS_DIR   = EVAL_DIR / "datasets"
RESULTS_DIR    = EVAL_DIR / "results"
RESULTS_DIR.mkdir(exist_ok=True)
APPSETTINGS_PATH = Path(__file__).parent.parent / "Dict" / "appsettings.Development.json"
BACKEND_DIR      = Path(__file__).parent.parent / "Dict"

load_dotenv(EVAL_DIR / ".env")
API_BASE   = os.environ["API_BASE_URL"].rstrip("/")
TOKEN      = os.environ["API_TOKEN"]
PROJECT_ID = int(os.environ["PROJECT_ID"])
DELAY      = float(os.environ.get("SWEEP_DELAY", os.environ.get("DELAY_BETWEEN", "4")))
OLLAMA_BASE = os.environ.get("OLLAMA_BASE_URL", "http://localhost:11434")

STREAM_URL = f"{API_BASE}/api/rag/project/{PROJECT_ID}/ask/stream"
HEADERS    = {"Authorization": f"Bearer {TOKEN}", "Content-Type": "application/json", "Accept": "text/event-stream"}


def check_ollama(wait: bool = True, max_wait: int = 120) -> bool:
    """Check Ollama is alive. If wait=True, block up to max_wait seconds for it to recover."""
    deadline = time.monotonic() + (max_wait if wait else 0)
    while True:
        try:
            r = requests.get(f"{OLLAMA_BASE}/api/tags", timeout=5)
            if r.status_code == 200:
                return True
        except Exception:
            pass
        if time.monotonic() >= deadline:
            return False
        print(f"  [ollama] not responding at {OLLAMA_BASE} — retrying in 10s...")
        time.sleep(10)

DATASET = "dev_set_sweep24"   # 24q stratified — better signal per trial for big sweep

# ── Param space ───────────────────────────────────────────────────────────────
# Only params that affect retrieval quality (not chunking — those require re-index)
RETRIEVAL_PARAMS = {
    "RetrievePerQuery":           (5,  30),
    "CandidatePoolLimit":         (20, 80),
    "RerankCandidateLimit":       (10, 30),
    "QueryVariantLimit":          (2,   6),
    "DecompositionSubQueryLimit": (1,   5),
    "RrfK":                       (20, 100),
    "Bm25K1":                     (0.8, 2.5),
    "Bm25B":                      (0.2, 1.0),
    "OutOfScopeScoreThreshold":   (0.38, 0.56),
    "ClarifyScoreThreshold":      (0.48, 0.62),
}

# Chunking params require re-index — sweep separately after retrieval is stable
CHUNKING_PARAMS = {
    "ChildChunkSize":    [400, 550, 700, 900],
    "ChildChunkOverlap": [60, 100, 140, 180],
    "ParentChunkSize":   [1000, 1400, 1600, 2000],
    "ParentChunkOverlap":[160, 240, 320],
}

# ── Helpers ───────────────────────────────────────────────────────────────────

HEALTH_URL = f"{API_BASE}/api/ocr/health"   # AllowAnonymous endpoint in OcrController
BACKEND_STARTUP_TIMEOUT = 90               # seconds to wait for backend to come up


def write_appsettings(params: dict):
    if APPSETTINGS_PATH.exists():
        existing = json.loads(APPSETTINGS_PATH.read_text(encoding="utf-8"))
    else:
        existing = {}
    existing.setdefault("RagTuning", {}).update(params)
    APPSETTINGS_PATH.write_text(json.dumps(existing, indent=2, ensure_ascii=False), encoding="utf-8")


def _wait_for_backend(timeout: int = BACKEND_STARTUP_TIMEOUT) -> bool:
    """Poll health endpoint until it responds or timeout."""
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        try:
            r = requests.get(HEALTH_URL, timeout=4, verify=False)
            if r.status_code < 500:
                return True
        except Exception:
            pass
        time.sleep(2)
    return False


def restart_backend(dry_run: bool = False):
    if dry_run:
        print("  [dry-run] skipping backend restart")
        return

    # 1. Find PID(s) of the running Dict backend by port
    kill_script = (
        "Get-NetTCPConnection -LocalPort 7084 -ErrorAction SilentlyContinue"
        " | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }"
    )
    subprocess.run(["powershell", "-Command", kill_script], capture_output=True)
    time.sleep(3)

    # 2. Start backend (no rebuild needed — params are in config, not compiled)
    log_file = BACKEND_DIR / "optuna_backend.log"
    with open(log_file, "a") as log:
        subprocess.Popen(
            ["dotnet", "run", "--no-build", "--project", str(BACKEND_DIR),
             "--launch-profile", "https"],
            cwd=str(BACKEND_DIR),
            stdout=log,
            stderr=log,
            creationflags=subprocess.CREATE_NEW_PROCESS_GROUP,
        )

    # 3. Wait for backend to be healthy
    if not _wait_for_backend():
        raise RuntimeError(
            f"Backend did not come up in {BACKEND_STARTUP_TIMEOUT}s. "
            f"Check {log_file} for errors."
        )

    # 4. Extra wait for ONNX embedding model to fully load
    # Health endpoint passes before ONNX is ready → queries return 0 sources
    print("  [backend] health OK — waiting 20s for ONNX model warm-up...")
    time.sleep(20)


def parse_sse(raw: str) -> dict:
    result = {"answer": "", "sources": []}
    for block in raw.split("\n\n"):
        for line in block.splitlines():
            if line.startswith("event:"):
                etype = line[6:].strip()
            elif line.startswith("data:"):
                data = line[5:].strip()
                if etype == "chunk":
                    result["answer"] += data
                elif etype in ("sources", "done"):
                    try:
                        p = json.loads(data)
                        if "sources" in p:
                            result["sources"] = p["sources"]
                        if "answer" in p:
                            result["answer"] = p["answer"]
                    except Exception:
                        pass
    return result


def call_rag(question: str, topK: int = 8) -> tuple[str, list, float]:
    t0 = time.monotonic()
    try:
        resp = requests.post(STREAM_URL, headers=HEADERS,
                             json={"question": question, "topK": topK, "mode": "high", "skipClarify": True},
                             timeout=180, verify=False, stream=True)
        if resp.status_code >= 500:
            print(f"  [call_rag] HTTP {resp.status_code} — backend error: {resp.text[:200]}")
            return "", [], 180.0
        if resp.status_code == 401:
            raise RuntimeError(f"JWT expired or invalid (401). Update API_TOKEN in .env")
        result = parse_sse(resp.text)
        latency = time.monotonic() - t0
        return result["answer"], result["sources"], latency
    except RuntimeError:
        raise
    except requests.exceptions.Timeout:
        print(f"  [call_rag] TIMEOUT after 180s on: {question[:60]}")
        return "", [], 180.0
    except requests.exceptions.ConnectionError as e:
        print(f"  [call_rag] CONNECTION ERROR (backend down?): {e}")
        return "", [], 180.0
    except Exception as e:
        print(f"  [call_rag] unexpected error: {type(e).__name__}: {e}")
        return "", [], 180.0


# ── Scoring ───────────────────────────────────────────────────────────────────

def normalize(text: str) -> str:
    text = unicodedata.normalize("NFKC", text).lower()
    return re.sub(r"\s+", " ", text).strip()


REFUSE_INDICATORS = [
    "không có thông tin", "không đề cập", "không tìm thấy thông tin",
    "nằm ngoài phạm vi", "từ chối", "không thể trả lời",
    "không liên quan", "không được đề cập", "không chứa thông tin",
    "chưa có tài liệu", "ngoài nội dung",
]


def is_refusal(answer: str) -> bool:
    a = normalize(answer)
    return not answer.strip() or any(normalize(p) in a for p in REFUSE_INDICATORS)


def point_hit(answer: str, point: str, threshold: float = 0.6) -> bool:
    a, p = normalize(answer), normalize(point)
    if p in a:
        return True
    if len(p) <= 20:
        return False
    tokens = [t for t in re.split(r"[\W_]+", p) if len(t) >= 3]
    if not tokens:
        return False
    return (sum(1 for t in tokens if t in a) / len(tokens)) >= threshold


def score_trial(cases: list, top_k: int = 8) -> dict:
    page_hits, point_recalls, refuse_ok, latencies = [], [], [], []

    for i, case in enumerate(cases):
        if i > 0:
            time.sleep(DELAY)
        print(f"    [{i+1}/{len(cases)}] {case['id']} {case['category'][:4]}...", end=" ", flush=True)
        answer, sources, latency = call_rag(case["question"], topK=top_k)
        print(f"{len(sources)}src {latency:.1f}s")
        latencies.append(latency)

        # page_hit
        expected_pages = case.get("source_pages", [])
        retrieved_pages = [s.get("pageNumber") for s in sources]
        if expected_pages:
            page_hits.append(int(any(p in retrieved_pages for p in expected_pages)))

        # refuse accuracy
        if case.get("must_refuse"):
            refuse_ok.append(int(is_refusal(answer)))
            continue

        # point_recall
        pts = case.get("expected_points", []) or []
        if pts:
            hits = [point_hit(answer, p) for p in pts]
            point_recalls.append(sum(hits) / len(hits))

        # Early abort: if first 5 in-domain cases all have 0 sources → backend broken
        in_domain_done = len(page_hits)
        if in_domain_done >= 5 and sum(page_hits[-5:]) == 0 and sum(latencies[-5:]) / 5 < 32:
            print("  [ABORT] first 5 in-domain all returned 0 sources + fast latency → backend not ready, pruning trial")
            return {"objective": 0.0, "page_hit_rate": 0.0, "point_recall": 0.0,
                    "refuse_accuracy": 0.0, "avg_latency": 0.0, "latency_penalty": 0.0,
                    "_aborted": True}

    latency_penalty = max(0, (sum(latencies) / len(latencies) - 8.0) * 0.01)

    page_hit_rate  = sum(page_hits)  / len(page_hits)  if page_hits  else 0
    point_recall   = sum(point_recalls) / len(point_recalls) if point_recalls else 0
    refuse_acc     = sum(refuse_ok) / len(refuse_ok) if refuse_ok else 1.0

    objective = 0.35 * page_hit_rate + 0.35 * point_recall + 0.3 * refuse_acc - latency_penalty
    return {
        "objective": objective,
        "page_hit_rate": round(page_hit_rate, 3),
        "point_recall": round(point_recall, 3),
        "refuse_accuracy": round(refuse_acc, 3),
        "avg_latency": round(sum(latencies) / len(latencies), 2),
        "latency_penalty": round(latency_penalty, 4),
    }


# ── Optuna objective ──────────────────────────────────────────────────────────

def make_objective(cases: list, dry_run: bool = False):
    def objective(trial: optuna.Trial) -> float:
        # Guard: Ollama must be alive before spending time on this trial
        if not check_ollama(wait=True, max_wait=120):
            print(f"  [Trial {trial.number}] SKIP — Ollama at {OLLAMA_BASE} did not respond after 120s")
            raise optuna.exceptions.TrialPruned()

        params = {
            "RetrievePerQuery":           trial.suggest_int("RetrievePerQuery",           *RETRIEVAL_PARAMS["RetrievePerQuery"]),
            "CandidatePoolLimit":         trial.suggest_int("CandidatePoolLimit",         *RETRIEVAL_PARAMS["CandidatePoolLimit"]),
            "RerankCandidateLimit":       trial.suggest_int("RerankCandidateLimit",       *RETRIEVAL_PARAMS["RerankCandidateLimit"]),
            "QueryVariantLimit":          trial.suggest_int("QueryVariantLimit",          *RETRIEVAL_PARAMS["QueryVariantLimit"]),
            "DecompositionSubQueryLimit": trial.suggest_int("DecompositionSubQueryLimit", *RETRIEVAL_PARAMS["DecompositionSubQueryLimit"]),
            "RrfK":                       trial.suggest_int("RrfK",                       *RETRIEVAL_PARAMS["RrfK"]),
            "Bm25K1":                     trial.suggest_float("Bm25K1",                   *RETRIEVAL_PARAMS["Bm25K1"]),
            "Bm25B":                      trial.suggest_float("Bm25B",                    *RETRIEVAL_PARAMS["Bm25B"]),
            "OutOfScopeScoreThreshold":   trial.suggest_float("OutOfScopeScoreThreshold", *RETRIEVAL_PARAMS["OutOfScopeScoreThreshold"]),
            "ClarifyScoreThreshold":      trial.suggest_float("ClarifyScoreThreshold",    *RETRIEVAL_PARAMS["ClarifyScoreThreshold"]),
        }
        top_k = trial.suggest_int("topK", 5, 20)

        print(f"\n[Trial {trial.number}] params={json.dumps({k: round(v, 3) if isinstance(v, float) else v for k, v in params.items()}, separators=(',', ':'))}")
        write_appsettings(params)
        restart_backend(dry_run=dry_run)

        metrics = score_trial(cases, top_k=top_k)
        print(f"  → obj={metrics['objective']:.4f}  page_hit={metrics['page_hit_rate']:.2%}  recall={metrics['point_recall']:.2%}  refuse={metrics['refuse_accuracy']:.2%}  latency={metrics['avg_latency']:.1f}s")
        trial.set_user_attr("metrics", metrics)
        return metrics["objective"]
    return objective


# ── Main ──────────────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--trials",        type=int,  default=20)
    parser.add_argument("--timeout",       type=int,  default=None, help="seconds total")
    parser.add_argument("--dry-run",       action="store_true", help="Skip backend restart (still calls API)")
    parser.add_argument("--max-questions", type=int,  default=None, help="Limit questions per trial for faster iteration")
    parser.add_argument("--suggest-only",  action="store_true", help="Print next suggested params, write to appsettings, then exit")
    parser.add_argument("--storage",       default=None, help="Optuna DB (e.g. sqlite:///sweep.db) — enables resume")
    parser.add_argument("--validate",      action="store_true", help="Smoke test: check health, call 3 questions, confirm scoring works")
    args = parser.parse_args()

    cases = [json.loads(l) for l in (DATASETS_DIR / f"{DATASET}.jsonl").read_text(encoding="utf-8").splitlines() if l.strip()]

    # ── Validate mode — smoke test before committing overnight ────────────────
    if args.validate:
        print("[validate] Checking health endpoint...")
        if not _wait_for_backend(timeout=10):
            print(f"  FAIL: backend not responding at {HEALTH_URL}")
            print("  → Start the backend first, then re-run --validate")
            return
        print(f"  OK: {HEALTH_URL}")

        print("[validate] Calling 3 questions from dev_set...")
        test_cases = cases[:3]
        metrics = score_trial(test_cases, top_k=8)
        print(f"  page_hit={metrics['page_hit_rate']:.0%}  recall={metrics['point_recall']:.0%}  latency={metrics['avg_latency']:.1f}s  obj={metrics['objective']:.4f}")

        print("[validate] Testing restart_backend (dry_run=True)...")
        restart_backend(dry_run=True)

        print("[validate] Checking appsettings write...")
        write_appsettings({"_validate_test": True})
        cfg = json.loads(APPSETTINGS_PATH.read_text(encoding="utf-8"))
        assert cfg.get("RagTuning", {}).get("_validate_test") is True
        # clean up test key
        cfg["RagTuning"].pop("_validate_test")
        APPSETTINGS_PATH.write_text(json.dumps(cfg, indent=2, ensure_ascii=False), encoding="utf-8")
        print(f"  OK: {APPSETTINGS_PATH}")

        print("\n✓ All checks passed — safe to run overnight:")
        n = len(cases)
        restart_sec = 35
        q_sec = 8
        for trials in [20, 50]:
            est = (n * q_sec + restart_sec) * trials / 60
            print(f"  --trials {trials}  ≈ {est:.0f} min  ({est/60:.1f} hours)")
        print(f"\nRun overnight:")
        print(f"  python optuna_sweep.py --trials 30 --storage sqlite:///sweep.db")
        return

    if args.max_questions:
        cases = cases[:args.max_questions]
        print(f"[optuna_sweep] ⚠ Using first {len(cases)} questions (--max-questions)")

    print(f"[optuna_sweep] dataset={DATASET}  questions={len(cases)}  trials={args.trials}")
    print(f"  appsettings → {APPSETTINGS_PATH}")
    est = len(cases) * 8 * args.trials / 60
    print(f"  Estimated time: ~{est:.0f} min ({len(cases)}q × ~8s × {args.trials} trials)")
    if args.dry_run:
        print("  DRY RUN — backend will not be restarted")

    study = optuna.create_study(
        direction="maximize",
        study_name="rag_retrieval_sweep",
        storage=args.storage,
        load_if_exists=True,
        sampler=optuna.samplers.TPESampler(seed=42),
    )

    if args.suggest_only:
        trial = study.ask()
        params = {
            "RetrievePerQuery":           trial.suggest_int("RetrievePerQuery",           *RETRIEVAL_PARAMS["RetrievePerQuery"]),
            "CandidatePoolLimit":         trial.suggest_int("CandidatePoolLimit",         *RETRIEVAL_PARAMS["CandidatePoolLimit"]),
            "RerankCandidateLimit":       trial.suggest_int("RerankCandidateLimit",       *RETRIEVAL_PARAMS["RerankCandidateLimit"]),
            "QueryVariantLimit":          trial.suggest_int("QueryVariantLimit",          *RETRIEVAL_PARAMS["QueryVariantLimit"]),
            "DecompositionSubQueryLimit": trial.suggest_int("DecompositionSubQueryLimit", *RETRIEVAL_PARAMS["DecompositionSubQueryLimit"]),
            "RrfK":                       trial.suggest_int("RrfK",                       *RETRIEVAL_PARAMS["RrfK"]),
            "Bm25K1":                     trial.suggest_float("Bm25K1",                   *RETRIEVAL_PARAMS["Bm25K1"]),
            "Bm25B":                      trial.suggest_float("Bm25B",                    *RETRIEVAL_PARAMS["Bm25B"]),
            "OutOfScopeScoreThreshold":   trial.suggest_float("OutOfScopeScoreThreshold", *RETRIEVAL_PARAMS["OutOfScopeScoreThreshold"]),
            "ClarifyScoreThreshold":      trial.suggest_float("ClarifyScoreThreshold",    *RETRIEVAL_PARAMS["ClarifyScoreThreshold"]),
        }
        top_k = trial.suggest_int("topK", 5, 20)
        write_appsettings({**params, "TopK": top_k})
        print(f"\nSuggested params (trial #{trial.number}):")
        print(json.dumps({**params, "topK": top_k}, indent=2))
        print(f"\n→ Written to {APPSETTINGS_PATH}")
        print("→ Rebuild + restart backend, then:")
        print("    python run_eval.py --dataset dev_set && python score_eval.py --dataset dev_set")
        return

    study.optimize(
        make_objective(cases, dry_run=args.dry_run),
        n_trials=args.trials,
        timeout=args.timeout,
        show_progress_bar=True,
    )

    best = study.best_trial
    print(f"\n{'='*60}")
    print(f"Best trial: #{best.number}  objective={best.value:.4f}")
    print(f"Best params:\n{json.dumps(best.params, indent=2)}")
    metrics = best.user_attrs.get("metrics", {})
    if metrics:
        print(f"  page_hit={metrics.get('page_hit_rate',0):.2%}  recall={metrics.get('point_recall',0):.2%}  refuse={metrics.get('refuse_accuracy',0):.2%}  latency={metrics.get('avg_latency',0):.1f}s")
    print(f"{'='*60}")
    write_appsettings(best.params)
    print(f"\nBest params written to {APPSETTINGS_PATH}")
    print("→ Rebuild backend then run final eval on test_set:")
    print("    python run_eval.py --dataset test_set && python score_eval.py --dataset test_set --verbose")


if __name__ == "__main__":
    main()
