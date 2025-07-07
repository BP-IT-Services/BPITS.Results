import { inject, Injectable } from '@angular/core';
import { SampleApiClient } from '../api/sample-api-client';
import { Observable } from 'rxjs';
import { SampleApiResult } from '../api/sample-api-result';
import { isSampleJob, SampleJob } from '../models/sample-job';
import { CreateSampleJobRequest } from '../models/create-sample-job-request';

@Injectable({ providedIn: 'root' })
export class SampleJobService {
  private readonly _api = inject(SampleApiClient);

  public async getJobAsync(jobId: string,
                           cancelRequest$?: Observable<unknown>): Promise<SampleApiResult<SampleJob>> {
    try {
      const url = `jobs/v1/${encodeURIComponent(jobId)}`;
      return await this._api.getAsync(url, isSampleJob, undefined, cancelRequest$);
    } catch (err) {
      console.error("Failed to get Job!", err);
      return this._api.handleRequestError(err);
    }
  }

  public async createJobAsync(request: CreateSampleJobRequest,
                              cancelRequest$?: Observable<unknown>): Promise<SampleApiResult<SampleJob>> {
    try {
      const url = "jobs/v1/";
      return await this._api.postAsync(url, request, isSampleJob, undefined, cancelRequest$)
    } catch (err) {
      console.error("Create Job request failed!", err);
      return this._api.handleRequestError(err);
    }
  }
}
