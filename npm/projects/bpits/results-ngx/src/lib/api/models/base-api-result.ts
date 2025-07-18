export type BaseApiResult<T, TResultStatusEnum> = {
  statusCode: TResultStatusEnum;
  value: T | null;
  errorMessage: string | null;
  errorDetails: Record<string, string[]> | null;
}
