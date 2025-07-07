import { Component, inject } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { BaseApiValidatedFormManager, FormFieldComponent } from '@bpits/results-ngx';
import { SampleJobService } from '../../services/sample-job.service';
import { SampleApiResultStatusCode } from '../../api/sample-result-status-code';
import { handleCommonApiErrors, IMessageService } from '../../api/sample-utils';

@Component({
  selector: 'sample-regular-form',
  templateUrl: './regular-form.component.html',
  imports: [
    ReactiveFormsModule,
    FormFieldComponent
  ],
  styleUrl: './regular-form.component.less'
})
export class RegularFormComponent {
  private readonly _sampleJobService = inject(SampleJobService);
  private readonly _formBuilder = inject(FormBuilder);
  public readonly formManager = new BaseApiValidatedFormManager(this._formBuilder.group({
    name: new FormControl<string | null>('', [Validators.required]),
    calendarColorHex: new FormControl<string | null>('', [Validators.required, Validators.pattern('^#(?:[0-9a-fA-F]{3}){1,2}$')]),
    lengthDays: new FormControl<number | null>(0, [Validators.required]),
    notes: new FormControl<string | null>(null),
  }));

  public get formGroup() {
    return this.formManager.formGroup;
  }

  public async submitAsync() {
    if(!this.formManager.validate())
      return;

    const formValues = this.formManager.formGroup.value;
    const result = await this._sampleJobService.createJobAsync({
      name: formValues.name ?? '',
      lengthDays: formValues.lengthDays ?? 0,
      calendarColorHex: formValues.calendarColorHex ?? '',
      notes: formValues.notes ?? '',
    });

    if(result.statusCode === SampleApiResultStatusCode.Ok && result.value) {
      // Handle success!
      return;
    }

    // Handle any custom error scenarios
    const messageHandler = {} as IMessageService; // Realistically, you'd get this from inject(...)

    switch(result.statusCode) {
      case SampleApiResultStatusCode.InsufficientPermissions:
        messageHandler.add('warn', 'Insufficient permissions', {
          detail: 'You lack the necessary permissions to create jobs in this area.'
        });
        return;
    }


    // Otherwise, fallback to the default API error handler
    handleCommonApiErrors(result, { messageService: messageHandler})
  }
}
