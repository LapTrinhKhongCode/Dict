export function getDaysUntil(target: string | Date): number {
  const targetDate = new Date(target)
  const today = new Date()

  // reset hours to avoid partial day issues
  targetDate.setHours(0, 0, 0, 0)
  today.setHours(0, 0, 0, 0)

  const diffTime = targetDate.getTime() - today.getTime()
  return Math.max(Math.ceil(diffTime / (1000 * 60 * 60 * 24)), 0)
}
