import { ApiClient } from '../../lib/api';
import { of } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { SampleApiResultStatusCode, SampleApiResultStatusCodeProvider } from '../sample-result-status-code';
import { CustomApiResult } from '../sample-api-result';

export class CustomApiClient extends ApiClient<SampleApiResultStatusCode> {
  constructor(http: HttpClient) {
    super(http, new SampleApiResultStatusCodeProvider());
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
