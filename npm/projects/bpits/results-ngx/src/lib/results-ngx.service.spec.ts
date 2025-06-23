import { TestBed } from '@angular/core/testing';

import { ResultsNgxService } from './results-ngx.service';

describe('ResultsNgxService', () => {
  let service: ResultsNgxService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ResultsNgxService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
