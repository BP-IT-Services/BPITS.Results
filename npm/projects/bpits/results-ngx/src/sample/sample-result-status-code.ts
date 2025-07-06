import { ICustomStatusCodeProvider } from '../lib/api';

export enum SampleApiResultStatusCode {
  Ok = 1,
  GenericFailure = 2,
  BadRequest = 3,
  AuthenticationTokenInvalid = 5,

  RequestCancelled = 65533,
  UnexpectedFormat = 65534,
  ServerUnreachable = 65535,
}

export class SampleApiResultStatusCodeProvider implements ICustomStatusCodeProvider<SampleApiResultStatusCode> {
  public readonly serverUnreachable = SampleApiResultStatusCode.ServerUnreachable;
  public readonly unexpectedFormat = SampleApiResultStatusCode.UnexpectedFormat;
  public readonly requestCancelled = SampleApiResultStatusCode.RequestCancelled;
  public readonly badRequest = SampleApiResultStatusCode.BadRequest;
  public readonly authenticationTokenInvalid = SampleApiResultStatusCode.AuthenticationTokenInvalid;
  public readonly genericFailure = SampleApiResultStatusCode.GenericFailure;
}
