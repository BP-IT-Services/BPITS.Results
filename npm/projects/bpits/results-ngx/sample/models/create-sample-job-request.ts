export type CreateSampleJobRequest = {
  name: string;
  age: number;
  details: Record<string, string> | null;
}
