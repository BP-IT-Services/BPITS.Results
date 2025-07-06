import { BaseApiValidatedFormManager, GenericFormGroup } from '../../lib/forms';
import { SampleApiResultStatusCode } from '../sample-result-status-code';

export class SampleApiValidatedFormManager<TFormGroup extends GenericFormGroup<TFormGroup>>
  extends BaseApiValidatedFormManager<TFormGroup, SampleApiResultStatusCode> {



}
