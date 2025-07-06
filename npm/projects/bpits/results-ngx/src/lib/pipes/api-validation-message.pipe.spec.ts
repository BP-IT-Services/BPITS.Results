import { ApiValidationMessagePipe } from './api-validation-message.pipe';

describe('ApiValidationExtractorPipe', () => {
  it('create an instance', () => {
    const pipe = new ApiValidationMessagePipe();
    expect(pipe).toBeTruthy();
  });
});
