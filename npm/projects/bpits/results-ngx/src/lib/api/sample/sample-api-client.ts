import { ApiResult } from '../models/api-result';
import { ICustomStatusCodeProvider } from '../api-status-codes';
import { ApiClient } from '../api-client';
import { of } from 'rxjs';
import { HttpClient } from '@angular/common/http';

export enum CustomApiResultStatusCode {
  Ok = 1,
  GenericFailure = 2,
  BadRequest = 3,
  AuthenticationTokenInvalid = 5,

  RequestCancelled = 65533,
  UnexpectedFormat = 65534,
  ServerUnreachable = 65535,
}

export type CustomApiResult<T> = ApiResult<T, CustomApiResultStatusCode>;

export class CustomApiResultStatusCodeProvider implements ICustomStatusCodeProvider<CustomApiResultStatusCode> {
  public readonly serverUnreachable = CustomApiResultStatusCode.ServerUnreachable;
  public readonly unexpectedFormat = CustomApiResultStatusCode.UnexpectedFormat;
  public readonly requestCancelled = CustomApiResultStatusCode.RequestCancelled;
  public readonly badRequest = CustomApiResultStatusCode.BadRequest;
  public readonly authenticationTokenInvalid = CustomApiResultStatusCode.AuthenticationTokenInvalid;
  public readonly genericFailure = CustomApiResultStatusCode.GenericFailure;
}

export class CustomApiClient extends ApiClient<CustomApiResultStatusCode> {
  constructor(http: HttpClient) {
    super(http, new CustomApiResultStatusCodeProvider());
  }
}


export class TestConsumer {
  constructor(private readonly _apiClient: CustomApiClient) {
  }

  async doSomething() {
    const result: CustomApiResult<TestType> = await this._apiClient.getAsync("", isTestGuard, undefined, of());
  }
}

export type TestType = {};
export function isTestGuard(obj: unknown): obj is TestType {
  return true;
}
