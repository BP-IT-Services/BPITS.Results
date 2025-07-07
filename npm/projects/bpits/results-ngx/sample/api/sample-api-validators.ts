import { BaseApiValidatedFormManager, GenericFormGroup } from '../../src/lib/forms';
import { SampleApiResultStatusCode } from './sample-result-status-code';
import { BaseApiValidatedStepFormManager } from '@bpits/results-ngx';

export class SampleApiValidatedFormManager<TFormGroup extends GenericFormGroup<TFormGroup>>
  extends BaseApiValidatedFormManager<TFormGroup, SampleApiResultStatusCode> {
  // Inherit base behaviour and extend/override if necessary
  // ...
}

export class SampleApiValidatedStepFormManager<TFormGroup extends GenericFormGroup<TFormGroup>> extends BaseApiValidatedStepFormManager<TFormGroup, SampleApiResultStatusCode> {
  // Inherit base behaviour and extend/override if necessary
  // ...
}
