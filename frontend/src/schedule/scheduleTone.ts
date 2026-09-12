import type { ScheduleItem } from './scheduleApi.ts'

export function scheduleTone(item: ScheduleItem) {
  if (item.deliveryMode === 1) return 'individual'
  return programTone(item.programId ?? item.groupId ?? item.id)
}

export function programTone(key: string) {
  let hash = 0
  for (const character of key) hash = (hash * 31 + character.charCodeAt(0)) | 0
  return `program-${Math.abs(hash) % 5 + 1}`
}
