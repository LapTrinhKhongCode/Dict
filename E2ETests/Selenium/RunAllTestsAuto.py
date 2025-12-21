"""
Runner Script (No Input): Chạy tất cả các bài test Selenium tự động.
Script này sẽ override tearDown để không chờ input().
Tạo báo cáo HTML sau khi chạy xong.

Cách chạy: python RunAllTestsAuto.py
Output: report.html
"""

import unittest
import sys
from datetime import datetime
import os
import time as time_module
import traceback
import re

# Patch để bỏ input() trong tearDown
original_input = __builtins__["input"] if isinstance(__builtins__, dict) else __builtins__.input

def mock_input(prompt=""):
    """Mock input() để không chờ user."""
    print(f"[AUTO] Skipping input prompt: {prompt}")
    return ""

# Áp dụng mock
import builtins
builtins.input = mock_input

# Import tất cả các test module (sau khi mock)
from LoginTest import AdminLoginTest
from LoginTestWrongPassword import LoginTestWrongPassword
from LoginTestMissingField import LoginTestMissingPassword
from CreateDeckTest import CreateDeckTest
from EditDeckTest import EditDeckTest
from CreateDeckWithImportTest import CreateDeckWithImportTest
from CreateDeckWithImportInvalidTest import CreateDeckWithImportInvalidTest
from CreateDeckWithImportTabTest import CreateDeckWithImportTabTest
from CreateDeckWithImportTabInvalidTest import CreateDeckWithImportTabInvalidTest
from CreateAndDeleteDeckTest import CreateAndDeleteDeckTest


# Custom TestResult to track individual test durations
class TimedTestResult(unittest.TestResult):
    """Custom TestResult that tracks test durations and cleaner error messages."""
    
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.test_timings = {}  # {test_name: duration_seconds}
        self.test_start_times = {}
        self.clean_errors = {}  # {test_name: {'summary': str, 'details': str}}
    
    def startTest(self, test):
        super().startTest(test)
        self.test_start_times[str(test)] = time_module.time()
    
    def stopTest(self, test):
        super().stopTest(test)
        test_name = str(test)
        if test_name in self.test_start_times:
            duration = time_module.time() - self.test_start_times[test_name]
            self.test_timings[test_name] = duration
    
    def addFailure(self, test, err):
        super().addFailure(test, err)
        self._extract_clean_error(test, err, 'FAILED')
    
    def addError(self, test, err):
        super().addError(test, err)
        self._extract_clean_error(test, err, 'ERROR')
    
    def _extract_clean_error(self, test, err, error_type):
        """Extract a clean, readable error message."""
        test_name = str(test)
        exc_type, exc_value, exc_tb = err
        
        # Get the error message
        error_msg = str(exc_value) if exc_value else str(exc_type.__name__)
        
        # Extract relevant info from error message
        summary = error_msg
        
        # For Selenium errors, extract the main message
        if 'Message:' in error_msg:
            match = re.search(r'Message:\s*(.+?)(?:\n|Stacktrace|$)', error_msg, re.DOTALL)
            if match:
                summary = match.group(1).strip()[:200]
        
        # For assertion errors, show the assertion message
        if exc_type.__name__ == 'AssertionError':
            summary = f"Assertion Failed: {error_msg[:200]}"
        
        # Get just the last few lines of the traceback (where the error actually occurred)
        tb_lines = traceback.format_exception(exc_type, exc_value, exc_tb)
        relevant_lines = []
        for line in tb_lines:
            if 'Selenium' in line or 'test_' in line or 'self.' in line:
                relevant_lines.append(line.strip())
        
        details = '\n'.join(relevant_lines[-5:]) if relevant_lines else error_msg[:500]
        
        self.clean_errors[test_name] = {
            'type': error_type,
            'summary': summary[:300],
            'details': details[:1000]
        }


# Danh sách test với thông tin mô tả
TEST_INFO = [
    (AdminLoginTest, 'test_successful_admin_login', 'Login thành công với tài khoản admin'),
    (LoginTestWrongPassword, 'test_login_wrong_password', 'Login thất bại với mật khẩu sai'),
    (LoginTestMissingPassword, 'test_login_wrong_password', 'Login thất bại khi thiếu trường'),
    (CreateDeckTest, 'test_successful_deck_creation', 'Tạo bộ thẻ mới (thủ công)'),
    (EditDeckTest, 'test_edit_deck_and_add_card', 'Chỉnh sửa bộ thẻ và thêm thẻ'),
    (CreateDeckWithImportTest, 'test_create_deck_with_import_comma_delimiter', 'Import thẻ với dấu phẩy (hợp lệ)'),
    (CreateDeckWithImportInvalidTest, 'test_import_with_invalid_format_no_comma', 'Import thẻ với dấu phẩy (không hợp lệ)'),
    (CreateDeckWithImportTabTest, 'test_create_deck_with_import_tab_delimiter', 'Import thẻ với Tab (hợp lệ)'),
    (CreateDeckWithImportTabInvalidTest, 'test_import_tab_with_invalid_format_no_tab', 'Import thẻ với Tab (không hợp lệ)'),
    (CreateAndDeleteDeckTest, 'test_create_and_delete_deck', 'Tạo và xóa bộ thẻ'),
]


def create_test_suite():
    """Tạo test suite với thứ tự cụ thể."""
    suite = unittest.TestSuite()
    for test_class, test_method, _ in TEST_INFO:
        suite.addTest(test_class(test_method))
    return suite


def generate_html_report(result, start_time, end_time, output_file='report.html'):
    """Tạo báo cáo HTML từ kết quả test."""
    
    passed = result.testsRun - len(result.failures) - len(result.errors)
    failed = len(result.failures)
    errors = len(result.errors)
    total = result.testsRun
    
    # Tính thời gian chạy
    duration = (end_time - start_time).total_seconds()
    
    # Get timing and error info from TimedTestResult
    test_timings = getattr(result, 'test_timings', {})
    clean_errors = getattr(result, 'clean_errors', {})
    
    # Xác định status cho từng test
    test_results = []
    
    # Build lookup dictionaries with multiple key formats
    failed_tests = {}
    error_tests = {}
    
    for test, tb in result.failures:
        test_str = str(test)
        failed_tests[test_str] = tb
        if hasattr(test, '_testMethodName'):
            failed_tests[test._testMethodName] = tb
    
    for test, tb in result.errors:
        test_str = str(test)
        error_tests[test_str] = tb
        if hasattr(test, '_testMethodName'):
            error_tests[test._testMethodName] = tb
    
    for test_class, test_method, description in TEST_INFO:
        test_name_full = f"{test_method} ({test_class.__name__})"
        test_name_module = f"{test_method} ({test_class.__module__}.{test_class.__name__})"
        
        status = 'PASSED'
        status_class = 'passed'
        error_summary = ''
        error_details = ''
        
        # Check if this test failed or errored
        matched_key = None
        for key in [test_name_full, test_name_module, test_method]:
            if key in failed_tests:
                status = 'FAILED'
                status_class = 'failed'
                matched_key = key
                break
            elif key in error_tests:
                status = 'ERROR'
                status_class = 'error'
                matched_key = key
                break
        
        # Get clean error info
        if matched_key:
            for err_key in [test_name_full, test_name_module]:
                if err_key in clean_errors:
                    error_summary = clean_errors[err_key].get('summary', '')
                    error_details = clean_errors[err_key].get('details', '')
                    break
        
        # Get test duration
        test_duration = None
        for timing_key in [test_name_full, test_name_module]:
            if timing_key in test_timings:
                test_duration = test_timings[timing_key]
                break
        
        test_results.append({
            'name': test_method,
            'class': test_class.__name__,
            'description': description,
            'status': status,
            'status_class': status_class,
            'error_summary': error_summary,
            'error_details': error_details,
            'duration': test_duration
        })
    
    # Tính tỷ lệ pass
    pass_rate = (passed / total * 100) if total > 0 else 0
    
    html_content = f'''<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Selenium E2E Test Report</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
            min-height: 100vh;
            color: #eee;
            padding: 20px;
        }}
        .container {{
            max-width: 1200px;
            margin: 0 auto;
        }}
        .header {{
            text-align: center;
            padding: 30px;
            background: rgba(255,255,255,0.05);
            border-radius: 16px;
            margin-bottom: 30px;
            backdrop-filter: blur(10px);
        }}
        .header h1 {{
            font-size: 2.5rem;
            background: linear-gradient(90deg, #00d2ff, #3a7bd5);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            margin-bottom: 10px;
        }}
        .header p {{
            color: #888;
            font-size: 0.9rem;
        }}
        .summary {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
            gap: 20px;
            margin-bottom: 30px;
        }}
        .stat-card {{
            background: rgba(255,255,255,0.05);
            border-radius: 12px;
            padding: 20px;
            text-align: center;
            backdrop-filter: blur(10px);
            border: 1px solid rgba(255,255,255,0.1);
        }}
        .stat-card.passed {{ border-left: 4px solid #00c853; }}
        .stat-card.failed {{ border-left: 4px solid #ff5252; }}
        .stat-card.error {{ border-left: 4px solid #ff9800; }}
        .stat-card.total {{ border-left: 4px solid #2196f3; }}
        .stat-card.time {{ border-left: 4px solid #9c27b0; }}
        .stat-value {{
            font-size: 2.5rem;
            font-weight: bold;
            margin-bottom: 5px;
        }}
        .stat-card.passed .stat-value {{ color: #00c853; }}
        .stat-card.failed .stat-value {{ color: #ff5252; }}
        .stat-card.error .stat-value {{ color: #ff9800; }}
        .stat-card.total .stat-value {{ color: #2196f3; }}
        .stat-card.time .stat-value {{ color: #9c27b0; font-size: 1.5rem; }}
        .stat-label {{
            color: #888;
            font-size: 0.85rem;
            text-transform: uppercase;
            letter-spacing: 1px;
        }}
        .progress-bar {{
            background: rgba(255,255,255,0.1);
            border-radius: 10px;
            height: 20px;
            margin: 20px 0;
            overflow: hidden;
        }}
        .progress-fill {{
            height: 100%;
            background: linear-gradient(90deg, #00c853, #69f0ae);
            border-radius: 10px;
            transition: width 0.5s ease;
        }}
        .tests-section {{
            background: rgba(255,255,255,0.05);
            border-radius: 16px;
            padding: 25px;
            backdrop-filter: blur(10px);
        }}
        .tests-section h2 {{
            margin-bottom: 20px;
            font-size: 1.5rem;
            color: #fff;
        }}
        .test-item {{
            background: rgba(0,0,0,0.2);
            border-radius: 10px;
            padding: 15px 20px;
            margin-bottom: 10px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            border-left: 4px solid transparent;
            transition: transform 0.2s;
        }}
        .test-item:hover {{
            transform: translateX(5px);
        }}
        .test-item.passed {{ border-left-color: #00c853; }}
        .test-item.failed {{ border-left-color: #ff5252; }}
        .test-item.error {{ border-left-color: #ff9800; }}
        .test-info {{
            flex: 1;
        }}
        .test-name {{
            font-weight: 600;
            color: #fff;
            margin-bottom: 5px;
        }}
        .test-desc {{
            font-size: 0.85rem;
            color: #888;
        }}
        .test-status {{
            padding: 6px 16px;
            border-radius: 20px;
            font-size: 0.8rem;
            font-weight: 600;
            text-transform: uppercase;
        }}
        .test-status.passed {{
            background: rgba(0, 200, 83, 0.2);
            color: #00c853;
        }}
        .test-status.failed {{
            background: rgba(255, 82, 82, 0.2);
            color: #ff5252;
        }}
        .test-status.error {{
            background: rgba(255, 152, 0, 0.2);
            color: #ff9800;
        }}
        .footer {{
            text-align: center;
            padding: 20px;
            color: #666;
            font-size: 0.8rem;
            margin-top: 30px;
        }}
        .error-details {{
            background: rgba(255, 82, 82, 0.1);
            border-radius: 8px;
            padding: 10px;
            margin-top: 10px;
            font-family: monospace;
            font-size: 0.75rem;
            color: #ff8a80;
            white-space: pre-wrap;
            word-break: break-all;
            max-height: 200px;
            overflow-y: auto;
        }}
        .test-duration {{
            background: rgba(156, 39, 176, 0.2);
            color: #ce93d8;
            padding: 2px 8px;
            border-radius: 12px;
            font-size: 0.75rem;
            margin-left: 10px;
            font-weight: normal;
        }}
        .error-summary {{
            background: rgba(255, 82, 82, 0.15);
            color: #ff8a80;
            padding: 8px 12px;
            border-radius: 6px;
            margin-top: 8px;
            font-size: 0.85rem;
            line-height: 1.4;
        }}
        .error-details-container {{
            margin: 10px 0;
            margin-left: 20px;
        }}
        .error-details-container summary {{
            cursor: pointer;
            color: #888;
            font-size: 0.8rem;
            padding: 5px;
        }}
        .error-details-container summary:hover {{
            color: #aaa;
        }}
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🧪 Selenium E2E Test Report</h1>
            <p>Dict Application - Flashcard Deck Management</p>
            <p style="margin-top: 10px;">
                📅 {start_time.strftime('%Y-%m-%d %H:%M:%S')} → {end_time.strftime('%H:%M:%S')}
            </p>
        </div>
        
        <div class="summary">
            <div class="stat-card passed">
                <div class="stat-value">{passed}</div>
                <div class="stat-label">Passed</div>
            </div>
            <div class="stat-card failed">
                <div class="stat-value">{failed}</div>
                <div class="stat-label">Failed</div>
            </div>
            <div class="stat-card error">
                <div class="stat-value">{errors}</div>
                <div class="stat-label">Errors</div>
            </div>
            <div class="stat-card total">
                <div class="stat-value">{total}</div>
                <div class="stat-label">Total</div>
            </div>
            <div class="stat-card time">
                <div class="stat-value">{duration:.1f}s</div>
                <div class="stat-label">Duration</div>
            </div>
        </div>
        
        <div class="progress-bar">
            <div class="progress-fill" style="width: {pass_rate:.1f}%"></div>
        </div>
        <p style="text-align: center; color: #888; margin-bottom: 30px;">
            Pass Rate: <strong style="color: #00c853;">{pass_rate:.1f}%</strong>
        </p>
        
        <div class="tests-section">
            <h2>📋 Test Results</h2>
'''
    
    for i, tr in enumerate(test_results, 1):
        # Format duration
        duration_str = ""
        if tr['duration'] is not None:
            duration_str = f"<span class='test-duration'>⏱ {tr['duration']:.1f}s</span>"
        
        html_content += f'''
            <div class="test-item {tr['status_class']}">
                <div class="test-info">
                    <div class="test-name">{i}. {tr['class']} {duration_str}</div>
                    <div class="test-desc">{tr['description']}</div>
'''
        # Add error summary if exists
        if tr['error_summary']:
            escaped_summary = tr['error_summary'].replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')
            html_content += f'''
                    <div class="error-summary">💥 {escaped_summary}</div>
'''
        
        html_content += f'''
                </div>
                <span class="test-status {tr['status_class']}">{tr['status']}</span>
            </div>
'''
        # Add collapsible error details if exists
        if tr['error_details']:
            escaped_details = tr['error_details'].replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')
            html_content += f'''
            <details class="error-details-container">
                <summary>📄 View Technical Details</summary>
                <div class="error-details">{escaped_details}</div>
            </details>
'''
    
    html_content += f'''
        </div>
        
        <div class="footer">
            <p>Generated by RunAllTestsAuto.py | Python {sys.version.split()[0]}</p>
        </div>
    </div>
</body>
</html>
'''
    
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(html_content)
    
    return output_file


def main():
    start_time = datetime.now()
    
    print("=" * 60)
    print("🚀 SELENIUM E2E TEST RUNNER (AUTO MODE)")
    print(f"📅 Thời gian bắt đầu: {start_time.strftime('%Y-%m-%d %H:%M:%S')}")
    print("=" * 60)
    print()
    
    suite = create_test_suite()
    
    print(f"📋 Tổng số test: {suite.countTestCases()}")
    print()
    print("Danh sách test sẽ chạy:")
    print("-" * 40)
    for i, (_, _, desc) in enumerate(TEST_INFO, 1):
        print(f"{i:2}. {desc}")
    print("-" * 40)
    print()
    
    # Chạy tests with TimedTestResult
    result = TimedTestResult()
    runner = unittest.TextTestRunner(verbosity=2, resultclass=TimedTestResult)
    result = runner.run(suite)
    
    end_time = datetime.now()
    
    # Tổng kết console
    print()
    print("=" * 60)
    print("📊 KẾT QUẢ TỔNG HỢP")
    print("=" * 60)
    passed = result.testsRun - len(result.failures) - len(result.errors)
    print(f"✅ Tests passed: {passed}")
    print(f"❌ Tests failed: {len(result.failures)}")
    print(f"⚠️  Tests error: {len(result.errors)}")
    print(f"📈 Tổng số tests: {result.testsRun}")
    print(f"📅 Thời gian kết thúc: {end_time.strftime('%Y-%m-%d %H:%M:%S')}")
    print("=" * 60)
    
    # Tạo HTML report
    report_file = generate_html_report(result, start_time, end_time)
    report_path = os.path.abspath(report_file)
    print(f"\n📄 HTML Report đã được tạo: {report_path}")
    
    if result.failures:
        print("\n❌ FAILED TESTS:")
        for test, _ in result.failures:
            print(f"  - {test}")
    
    if result.errors:
        print("\n⚠️  ERROR TESTS:")
        for test, _ in result.errors:
            print(f"  - {test}")
    
    if result.wasSuccessful():
        print("\n🎉 TẤT CẢ TESTS ĐỀU PASSED!")
        sys.exit(0)
    else:
        print("\n💥 CÓ TESTS THẤT BẠI!")
        sys.exit(1)


if __name__ == '__main__':
    main()
