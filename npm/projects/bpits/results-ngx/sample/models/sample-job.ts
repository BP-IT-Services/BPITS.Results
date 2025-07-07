export type SampleJob = {
  id: string;
  name: string;
  age: number;
  details: Record<string, string> | null;
}

export function isSampleJob(obj: unknown): obj is SampleJob {
  if (!obj || typeof (obj) !== 'object')
    return false;

  const recordObj = obj as Record<string, unknown>;
  return typeof recordObj["id"] === 'string'
    && typeof recordObj["name"] === 'string'
    && typeof recordObj["age"] === 'number'
    && (recordObj["details"] === null || typeof recordObj["details"] === 'object');
}
