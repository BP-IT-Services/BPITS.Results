export type SampleJob = {
  id: string;
  name: string;
  lengthDays: number;
  calendarColorHex: string;
  notes: string | null;
}

export function isSampleJob(obj: unknown): obj is SampleJob {
  if (!obj || typeof (obj) !== 'object')
    return false;

  const recordObj = obj as Record<string, unknown>;
  return typeof recordObj["id"] === 'string'
    && typeof recordObj["name"] === 'string'
    && typeof recordObj["lengthDays"] === 'number'
    && typeof recordObj["calendarColorHex"] === 'string'
    && (!recordObj["notes"] || typeof recordObj["notes"] === 'string');
}
