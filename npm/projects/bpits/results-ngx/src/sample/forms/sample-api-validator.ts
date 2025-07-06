import { AbstractControl } from '@angular/forms';
import {
  BaseApiValidatedFormManager,
  IApiValidatedFormManager
} from '../../lib/forms/managers/base-api-validated-form-manager';
import { SampleApiResultStatusCode } from '../sample-result-status-code';
import { GenericFormGroup } from '../../lib/forms/managers/generic-form-group.type';

export class SampleApiValidatedFormManager<TFormGroup extends GenericFormGroup<TFormGroup>>
  extends BaseApiValidatedFormManager<TFormGroup, SampleApiResultStatusCode> {



}
