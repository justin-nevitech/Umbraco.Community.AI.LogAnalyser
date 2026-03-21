import { html, css, nothing } from '@umbraco-cms/backoffice/external/lit';
import { customElement, state } from 'lit/decorators.js';
import { unsafeHTML } from 'lit/directives/unsafe-html.js';
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import { marked } from '@umbraco-cms/backoffice/external/marked';
import type { LogAiSummaryModalData } from './log-ai-summary.modal-token.js';

function getLevelColor(level: string): string {
  switch (level.toLowerCase()) {
    case 'error':
    case 'fatal':
      return 'var(--uui-color-danger, #d42054)';
    case 'warning':
      return 'var(--uui-color-warning, #f0ac00)';
    case 'information':
    case 'info':
      return 'var(--uui-color-positive, #2bc37c)';
    case 'debug':
    case 'verbose':
      return 'var(--uui-color-default, #68737d)';
    default:
      return 'var(--uui-color-default, #68737d)';
  }
}

// Configure marked: convert single newlines to <br>, open links in new tab
marked.use({
  breaks: true,
  renderer: {
    link({ href, text }) {
      const safeHref = (href ?? '').replace(/&/g, '&amp;').replace(/"/g, '&quot;');
      if (/^javascript:/i.test(safeHref)) return text ?? '';
      return `<a href="${safeHref}" target="_blank" rel="noopener">${text}</a>`;
    },
  },
});

@customElement('log-ai-summary-dialog')
export default class LogAiSummaryDialogElement extends UmbModalBaseElement<LogAiSummaryModalData, undefined> {
  static styles = css`
    :host {
      display: block;
      max-width: 800px;
      width: 90vw;
    }

    .log-meta {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 12px;
      font-size: 13px;
      color: var(--uui-color-text-alt, #68737d);
    }

    .level-badge {
      display: inline-block;
      padding: 2px 8px;
      border-radius: 3px;
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      color: #fff;
    }

    .log-message {
      background: var(--uui-color-surface-alt, #f4f3f5);
      border-radius: 4px;
      padding: 12px;
      font-family: monospace;
      font-size: 13px;
      line-height: 1.5;
      white-space: pre-wrap;
      word-break: break-word;
      margin-bottom: 16px;
      border-left: 3px solid var(--uui-color-border, #e9e7e8);
    }

    .divider {
      border: none;
      border-top: 1px solid var(--uui-color-border, #e9e7e8);
      margin: 16px 0;
      flex-shrink: 0;
    }

    .summary-label {
      font-size: 12px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: var(--uui-color-text-alt, #68737d);
      margin-bottom: 8px;
    }

    .summary-content {
      font-size: 14px;
      line-height: 1.6;
      max-height: 50vh;
      overflow-y: auto;
    }

    .summary-content h2,
    .summary-content h3,
    .summary-content h4 {
      margin: 12px 0 4px;
      font-size: 14px;
    }

    .summary-content ul {
      margin: 4px 0;
      padding-left: 20px;
    }

    .summary-content li {
      margin-bottom: 4px;
    }

    .summary-content code {
      background: var(--uui-color-surface-alt, #f4f3f5);
      padding: 1px 4px;
      border-radius: 3px;
      font-size: 12px;
    }

    .summary-content pre {
      background: var(--uui-color-surface-alt, #f4f3f5);
      border-radius: 4px;
      padding: 12px;
      overflow-x: auto;
      margin: 8px 0;
    }

    .summary-content pre code {
      background: none;
      padding: 0;
      border-radius: 0;
      font-size: 12px;
      line-height: 1.5;
      white-space: pre;
    }

    .summary-content a {
      color: var(--uui-color-interactive, #1b264f);
      text-decoration: underline;
    }

    .summary-content a:hover {
      color: var(--uui-color-interactive-emphasis, #303f9f);
    }

    .summary-content ol {
      margin: 4px 0;
      padding-left: 20px;
    }

    .summary-content p {
      margin: 0 0 8px;
    }

    .loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 12px;
      padding: 20px;
      color: var(--uui-color-text-alt, #68737d);
    }

    .loading-text {
      font-size: 13px;
    }

    .error {
      color: #fff;
      padding: 12px;
      background: var(--uui-color-danger, #d42054);
      border-radius: 4px;
      font-size: 13px;
    }
  `;

  @state() private _summary = '';
  @state() private _loading = true;
  @state() private _error = '';

  private _abortController?: AbortController;

  override connectedCallback() {
    super.connectedCallback();
    this._fetchSummary();
  }

  override disconnectedCallback() {
    super.disconnectedCallback();
    this._abortController?.abort();
  }

  private _fetchSummary = async () => {
    this._abortController?.abort();
    this._abortController = new AbortController();

    this._loading = true;
    this._error = '';

    try {
      const authContext = await this.getContext(UMB_AUTH_CONTEXT);
      if (!authContext) {
        this._error = 'Authentication context not available.';
        this._loading = false;
        return;
      }
      const token = await authContext.getLatestToken();

      const response = await fetch(
        '/umbraco/ailoganalyser/api/v1.0/analyse',
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`,
          },
          body: JSON.stringify({
            level: this.data?.level,
            timestamp: this.data?.timestamp,
            message: this.data?.message,
            messageTemplate: this.data?.messageTemplate || undefined,
            exception: this.data?.exception || undefined,
            properties: this.data?.properties || undefined,
          }),
          signal: this._abortController.signal,
        },
      );

      if (!response.ok) {
        const text = await response.text();
        throw new Error(
          `API returned ${response.status}: ${text.substring(0, 200)}`,
        );
      }

      const result = await response.json();
      this._summary = result.summary;
    } catch (err: unknown) {
      if (err instanceof DOMException && err.name === 'AbortError') return;
      this._error =
        err instanceof Error ? err.message : 'Failed to get AI summary.';
    } finally {
      this._loading = false;
    }
  };

  private _close() {
    this.modalContext?.reject();
  }

  render() {
    const levelColor = getLevelColor(this.data?.level ?? '');

    return html`
      <uui-dialog-layout headline="AI Log Analysis">
        <div class="content-wrapper">
          <div class="log-section">
            <div class="log-meta">
          ${this.data?.level
            ? html`<span
                class="level-badge"
                style="background:${levelColor}"
                >${this.data.level}</span
              >`
            : nothing}
          ${this.data?.timestamp
            ? html`<span>${this.data.timestamp}</span>`
            : nothing}
        </div>

        <div class="log-message">${this.data?.message}</div>
          </div>

          <hr class="divider" />

          <div class="summary-section">
            <div class="summary-label">AI Summary</div>

        ${this._loading
          ? html`
              <div class="loading">
                <uui-loader-bar></uui-loader-bar>
                <span class="loading-text"
                  >Analysing log entry with AI...</span
                >
              </div>
            `
          : this._error
            ? html`<div class="error">${this._error}</div>`
            : html`
                <div class="summary-content">
                  ${unsafeHTML(marked.parse(this._summary, { async: false }) as string)}
                    </div>
                  `}
          </div>
        </div>

        ${!this._loading && !this._error
          ? html`
              <uui-button
                slot="actions"
                look="secondary"
                label="Re-analyse"
                @click=${this._fetchSummary}
              >
                <uui-icon name="icon-refresh"></uui-icon>
                Re-analyse
              </uui-button>
            `
          : nothing}
        <uui-button
          slot="actions"
          look="primary"
          label="Close"
          @click=${this._close}
        >
          Close
        </uui-button>
      </uui-dialog-layout>
    `;
  }
}

declare global {
  interface HTMLElementTagNameMap {
    'log-ai-summary-dialog': LogAiSummaryDialogElement;
  }
}
