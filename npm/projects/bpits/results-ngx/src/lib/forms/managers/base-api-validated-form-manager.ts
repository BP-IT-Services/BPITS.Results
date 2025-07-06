import { ApiValidationMessagePipe } from '../../pipes';
import { Observable, Subject, Subscription } from 'rxjs';
import { FormGroup } from '@angular/forms';
import { BaseApiResult } from '../../api';
import { GenericFormGroup } from './generic-form-group.type';

export type FormValueChangedEvent = { hasChanged: boolean; }


export interface IApiValidatedFormManager<TResultStatusEnum = unknown> {
  get validationApiResult(): BaseApiResult<unknown, TResultStatusEnum> | null;
  get isSubmitted(): boolean;

  get formGroup(): FormGroup;

  get onFormSubmitted$(): Observable<void>;
  get onFormValueChanged$(): Observable<FormValueChangedEvent>;
  get onValidationApiResultChanged$(): Observable<BaseApiResult<unknown, TResultStatusEnum> | null>;

  trackFormChanges(enable: boolean): void
  resetTrackedChanges(): void;
  restoreTrackedChanges(): void;

  validate(): boolean;
  applyApiValidationErrors(apiResult: BaseApiResult<unknown, TResultStatusEnum>): void

  isFieldValid(fieldName: string): boolean;
  fieldHasError(field: string, error: string): boolean;
  getInvalidFieldClasses(fieldName: string, options?: { validClass?: string, invalidClass?: string }): string;

  onDestroy(): void;
}


export class BaseApiValidatedFormManager<TFormGroup extends GenericFormGroup<TFormGroup>, TResultStatusEnum = any>
  implements IApiValidatedFormManager<TResultStatusEnum>
{
  private readonly _formSubmittedSubject = new Subject<void>();
  private readonly _formValueChangedSubject = new Subject<FormValueChangedEvent>();
  private readonly _validationApiResultChangedSubject = new Subject<BaseApiResult<unknown, TResultStatusEnum> | null>();
  private _validationApiResult: BaseApiResult<unknown, TResultStatusEnum> | null = null;

  private _trackFormChanges: boolean = false;
  private _trackFormChangesSubscription: Subscription | null = null;
  private _trackedFormStateJson: string | null = null;

  private _isSubmitted: boolean = false;
  public get isSubmitted(): boolean {
    return this._isSubmitted;
  }

  public get validationApiResult(): BaseApiResult<unknown, TResultStatusEnum> | null {
    return this._validationApiResult;
  }

  public readonly onFormSubmitted$: Observable<void> = this._formSubmittedSubject.asObservable();
  public readonly onFormValueChanged$: Observable<FormValueChangedEvent> = this._formValueChangedSubject.asObservable();
  public readonly onValidationApiResultChanged$: Observable<BaseApiResult<unknown, TResultStatusEnum> | null> = this._validationApiResultChangedSubject.asObservable();

  constructor(public readonly formGroup: FormGroup<TFormGroup>) {
  }

  public onDestroy(): void {
    this._trackFormChangesSubscription?.unsubscribe();
  }

  public trackFormChanges(enable: boolean): void {
    this._trackFormChanges = enable;
    this.resetTrackedChanges();

    if (enable && !this._trackFormChangesSubscription) {
      this._trackFormChangesSubscription = this.formGroup.valueChanges.subscribe(() => this.onFormValueChanged());
    }

    if (!enable && this._trackFormChangesSubscription) {
      this._trackFormChangesSubscription.unsubscribe();
      this._trackFormChangesSubscription = null;
    }
  }

  public restoreTrackedChanges(): void {
    if (!this._trackFormChanges || !this._trackedFormStateJson) {
      console.warn("Requested to restore tracked changes - but it is either disabled or not available");
      return;
    }

    // eslint-disable-next-line @typescript-eslint/no-unsafe-argument
    this.formGroup.patchValue(JSON.parse(this._trackedFormStateJson));
  }

  public validate(): boolean {
    this._isSubmitted = true;
    this.formGroup.markAllAsTouched();
    this._formSubmittedSubject.next();

    return !this.formGroup.invalid;
  }

  public reset(): void {
    this.formGroup.reset();
    this.resetValidation();
    this.resetTrackedChanges();
  }

  public resetValidation(): void {
    this._isSubmitted = false;
    this.formGroup.markAsPristine();
    this.formGroup.markAsUntouched();
  }

  public applyApiValidationErrors(apiResult: BaseApiResult<unknown, TResultStatusEnum>): void {
    this._validationApiResult = apiResult;
    this._validationApiResultChangedSubject.next(this._validationApiResult);
  }

  public resetTrackedChanges(): void {
    this._trackedFormStateJson = JSON.stringify(this.formGroup.value);
    this.onFormValueChanged(); // Update the subscribers
  }

  public isFieldValid(fieldName: string): boolean {
    const formField = this.formGroup.get(fieldName);
    if (!formField)
      return false;

    if (this.isSubmitted && formField?.invalid && (formField?.dirty || formField?.touched))
      return false;

    if (this._validationApiResult && ApiValidationMessagePipe.hasError(this._validationApiResult, fieldName))
      return false;

    return true;
  }

  public fieldHasError(field: string, error: string): boolean {
    if (this.isFieldValid(field))
      return false;

    return !!this.formGroup.get(field)?.errors?.[error];
  }

  public getInvalidFieldClasses(fieldName: string, options?: {
    validClass?: string,
    invalidClass?: string
  }): string {
    if (this.isFieldValid(fieldName))
      return options?.validClass ?? '';

    return options?.invalidClass ?? 'ng-dirty ng-invalid';
  }


  private onFormValueChanged() {
    if (!this._trackFormChanges)
      return;

    const newStateJson = JSON.stringify(this.formGroup.value);
    this._formValueChangedSubject.next({
      hasChanged: newStateJson !== this._trackedFormStateJson
    });
  }
}
