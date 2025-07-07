import { ApiClient } from '../../src/lib/api';
import { HttpClient } from '@angular/common/http';
import { SampleApiResultStatusCode, SampleApiResultStatusCodeProvider } from './sample-result-status-code';

export class SampleApiClient extends ApiClient<SampleApiResultStatusCode> {
  constructor(http: HttpClient) {
    super(http, new SampleApiResultStatusCodeProvider());
  }

  // Inherit base behaviour and extend/override if necessary
  // ...
}
