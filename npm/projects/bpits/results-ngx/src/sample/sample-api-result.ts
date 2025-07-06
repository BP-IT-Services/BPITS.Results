import { BaseApiResult } from '../lib/api';
import { SampleApiResultStatusCode } from './sample-result-status-code';

export type CustomApiResult<T> = BaseApiResult<T, SampleApiResultStatusCode>;
