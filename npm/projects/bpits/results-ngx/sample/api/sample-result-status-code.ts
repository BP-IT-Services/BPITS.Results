import { ICustomStatusCodeProvider } from '../../src/lib/api';

// This enum should match the server-side equivalent
export enum SampleApiResultStatusCode {
  Ok = 1,
  GenericFailure = 2,
  BadRequest = 3,
  AuthenticationTokenInvalid = 5,

  // The following status codes are CLIENT-ONLY!
  // They can be used for providing accurate UX.
  RequestCancelled = 65533,
  UnexpectedFormat = 65534,
  ServerUnreachable = 65535,
}

export class SampleApiResultStatusCodeProvider implements ICustomStatusCodeProvider<SampleApiResultStatusCode> {
  // The following status codes are required and an equivalent value must be present in your enum.
  public readonly serverUnreachable = SampleApiResultStatusCode.ServerUnreachable;
  public readonly unexpectedFormat = SampleApiResultStatusCode.UnexpectedFormat;
  public readonly requestCancelled = SampleApiResultStatusCode.RequestCancelled;
  public readonly badRequest = SampleApiResultStatusCode.BadRequest;
  public readonly authenticationTokenInvalid = SampleApiResultStatusCode.AuthenticationTokenInvalid;
  public readonly genericFailure = SampleApiResultStatusCode.GenericFailure;
}
