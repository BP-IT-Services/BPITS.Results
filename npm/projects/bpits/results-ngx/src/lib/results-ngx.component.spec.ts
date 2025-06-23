import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ResultsNgxComponent } from './results-ngx.component';

describe('ResultsNgxComponent', () => {
  let component: ResultsNgxComponent;
  let fixture: ComponentFixture<ResultsNgxComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ResultsNgxComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ResultsNgxComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
