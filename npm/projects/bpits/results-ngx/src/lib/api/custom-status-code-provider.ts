export interface ICustomStatusCodeProvider<TResultStatusEnum> {
  get genericFailure(): TResultStatusEnum;
  get serverUnreachable(): TResultStatusEnum;
  get unexpectedFormat(): TResultStatusEnum;
  get requestCancelled(): TResultStatusEnum;
  get badRequest(): TResultStatusEnum;
  get authenticationTokenInvalid(): TResultStatusEnum;
}
