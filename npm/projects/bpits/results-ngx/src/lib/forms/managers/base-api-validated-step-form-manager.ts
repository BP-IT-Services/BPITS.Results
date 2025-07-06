import { AbstractControl, FormGroup } from "@angular/forms";
import { GenericFormGroup } from './generic-form-group.type';
import { BaseApiValidatedFormManager } from './base-api-validated-form-manager';
import { BaseApiResult } from '../../api';

export type GenericMap<TKey, TValue> = {
  [K in keyof TKey]: TValue
};

export type ControlStepMapValue = {
  control: string;
  step: number;
}

export class ControlStepMap<TControls> implements Iterable<ControlStepMapValue> {
  public readonly map: GenericMap<TControls, number>;

  public constructor(initialValues: GenericMap<TControls, number>) {
    this.map = initialValues;
  }

  [Symbol.iterator](): Iterator<ControlStepMapValue> {
    const entries = Object.entries(this.map) as [keyof TControls, number][];

    let index = 0;

    return {
      next: (): IteratorResult<ControlStepMapValue> => {
        if (index < entries.length) {
          return {
            done: false,
            value: {
              control: entries[index][0].toString(),
              step: entries[index++][1]
            }
          }
        }

        return { value: undefined, done: true };
      }
    };
  }

  public getStep<K extends keyof TControls>(formControlName: K): number {
    return this.map[formControlName];
  }

  public setStep<K extends keyof TControls>(formControlName: K, value: number): void {
    this.map[formControlName] = value;
  }
}

export class BaseApiValidatedStepFormManager<TFormGroup extends GenericFormGroup<TFormGroup>, TResultStatusEnum = any>
  extends BaseApiValidatedFormManager<TFormGroup, TResultStatusEnum> {

  constructor(
    formGroup: FormGroup<TFormGroup>,
    public readonly controlStepMap: ControlStepMap<TFormGroup>)
  {
    super(formGroup);
  }

  public findEarliestStepWithValidationError(apiError: BaseApiResult<unknown, TResultStatusEnum>): number | null {
    // TODO: this function does not count Reactive Forms validation errors
    if (!apiError.errorDetails)
      return null;

    for (let { control, step } of this.controlStepMap) {
      const key: string | undefined = Object.keys(apiError.errorDetails).find(e => e.toLowerCase() === control.toLowerCase());
      if (key)
        return step;
    }

    return null;
  }

  public validateCanMoveToNextStep(currentStep: number): boolean {
    if (this.validate()) // If it's valid, no blocker to movement
      return true;

    for (let { control, step } of this.controlStepMap) {
      const formControl = this.formGroup.get(control);
      if (step <= currentStep && formControl && !formControl.valid)
        return false;
    }

    return true;
  }
}
