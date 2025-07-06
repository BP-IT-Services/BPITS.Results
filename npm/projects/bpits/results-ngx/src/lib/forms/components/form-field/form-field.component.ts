import { Component, computed, Injector, input, OnInit, runInInjectionContext, signal, Signal } from '@angular/core';
import { ApiValidationMessagePipe } from "../../../pipes";
import { toSignal } from "@angular/core/rxjs-interop";
import { BaseApiResult } from "../../../api";
import { IApiValidatedFormManager } from '../../managers/base-api-validated-form-manager';

export type FormFieldErrorMessages = {
  [errorName: string]: string | null | undefined
};

@Component({
  selector: 'app-form-field',
  imports: [
    ApiValidationMessagePipe
  ],
  templateUrl: './form-field.component.html',
  styleUrl: './form-field.component.less'
})
export class FormFieldComponent implements OnInit {
  private _internalApiResult: Signal<BaseApiResult<unknown, unknown> | null> = signal(null);

  public readonly field = input.required<string>();
  public readonly formManager = input.required<IApiValidatedFormManager>()
  public readonly formErrorMessages = input<FormFieldErrorMessages>();
  public readonly globalFormErrorMessages = input<FormFieldErrorMessages>();

  public readonly formGroup = computed(() => this.formManager().formGroup);
  public readonly apiResult = computed(() => this._internalApiResult() ?? this.formManager().validationApiResult);
  public readonly hasErrors = computed(() => !!this.apiResult()?.errorDetails);

  constructor(private readonly _injector: Injector) {
  }

  public ngOnInit() {
    runInInjectionContext(this._injector, () => {
      this._internalApiResult = toSignal(this.formManager().onValidationApiResultChanged$, { initialValue: null });
    });
  }

  protected hasReactiveFormsError(): boolean {
    const fieldName = this.field();
    if (!this.formGroup()) {
      console.warn('No form group found for field - cannot show validation errors')
      return false;
    }

    const hasFieldErrors = Object.keys(this.formGroup()?.get(fieldName)?.errors ?? {}).length > 0;
    if (hasFieldErrors)
      return true;

    const hasGlobalErrors = Object.keys(this.formGroup()?.errors ?? {}).length > 0;
    const firstGlobalErrorKey = Object.keys(this.formGroup()?.errors ?? {})[0];
    const hasErrorMessageForError = hasGlobalErrors && !!this.globalFormErrorMessages()?.[firstGlobalErrorKey];

    return hasErrorMessageForError;
  }

  protected reactiveFormsError(): string | null {
    const fieldName = this.field();
    if (!this.formGroup())
      return null;

    // Field errors are more important, if available
    if(!this.formManager().isFieldValid(fieldName)) {
      const fieldErrorMessages = this.formErrorMessages();
      const fieldErrorKeys = Object.keys(this.formGroup()?.get(fieldName)?.errors ?? {});
      const respectiveFieldErrorMessage = fieldErrorMessages?.[fieldErrorKeys[0]];
      if (respectiveFieldErrorMessage) {
        return respectiveFieldErrorMessage;
      }
    }

    // Global errors take the least precedence
    const globalErrors = Object.keys(this.formGroup().errors ?? {});
    if(globalErrors && this.globalFormErrorMessages()) {
      return this.globalFormErrorMessages()?.[globalErrors[0]] ?? null;
    }

    return null;
  }
}
