export type BaseApiResult<T, TResultStatusEnum> = {
  statusCode: TResultStatusEnum;
  value: T | null | undefined;
  errorMessage: string | null | undefined;
  errorDetails: Record<string, string[]> | null | undefined;
}
