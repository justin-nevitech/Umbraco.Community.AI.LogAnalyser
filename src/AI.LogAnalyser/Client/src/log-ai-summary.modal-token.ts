import { UmbModalToken } from '@umbraco-cms/backoffice/modal';

export interface LogAiSummaryModalData {
  timestamp: string;
  level: string;
  message: string;
  messageTemplate?: string;
  exception?: string;
  properties?: string;
}

export const LOG_AI_SUMMARY_MODAL = new UmbModalToken<LogAiSummaryModalData, undefined>(
  'LogAiSummary.Modal',
  {
    modal: {
      type: 'dialog',
      size: 'medium',
    },
  },
);
