import { BaseApiResult } from '../../src/lib/api';
import { SampleApiResultStatusCode } from './sample-result-status-code';

export type SampleApiResult<T> = BaseApiResult<T, SampleApiResultStatusCode>;
