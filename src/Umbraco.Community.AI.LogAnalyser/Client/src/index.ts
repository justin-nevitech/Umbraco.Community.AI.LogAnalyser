import type { UmbEntryPointOnInit } from '@umbraco-cms/backoffice/extension-api';
import type { ManifestModal } from '@umbraco-cms/backoffice/modal';
import { UMB_MODAL_MANAGER_CONTEXT } from '@umbraco-cms/backoffice/modal';
import { LOG_AI_SUMMARY_MODAL } from './log-ai-summary.modal-token.js';

export const onInit: UmbEntryPointOnInit = (host, extensionRegistry) => {
  const modalManifest: ManifestModal = {
    type: 'modal',
    alias: 'LogAiSummary.Modal',
    name: 'Log AI Summary Modal',
    js: () => import('./log-ai-summary-dialog.element.js'),
  };
  extensionRegistry.register(modalManifest);

  const enhancer = new LogViewerEnhancer(host);
  enhancer.start();
};

interface LogData {
  timestamp: string;
  level: string;
  message: string;
  messageTemplate?: string;
  exception?: string;
  properties?: string;
}

const CELL_STYLE =
  'flex: 0 0 4ch; box-sizing: border-box; padding: 10px 20px; display: flex; align-items: center; justify-content: center; margin-left: auto;';

const AI_ICON_SVG =
  '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.75" viewBox="0 0 24 24"><path d="m21.64 3.64-1.28-1.28a1.21 1.21 0 0 0-1.72 0L2.36 18.64a1.21 1.21 0 0 0 0 1.72l1.28 1.28a1.2 1.2 0 0 0 1.72 0L21.64 5.36a1.2 1.2 0 0 0 0-1.72M14 7l3 3M5 6v4M19 14v4M10 2v2M7 8H3M21 16h-4M11 3H9"/></svg>';

/**
 * Enhances the Umbraco log viewer with an AI analysis button on each log row.
 * Targets the actual shadow DOM structure:
 *   umb-log-viewer-messages-list (shadow) → #header + #main → umb-log-viewer-message (shadow) → details > summary
 *
 * Uses polling because MutationObserver does not observe changes inside shadow roots.
 */
class LogViewerEnhancer {
  private _enhancedMessages = new WeakSet<Element>();
  private _cachedMessagesList: Element | null = null;
  private _host: typeof UMB_MODAL_MANAGER_CONTEXT.TYPE | undefined;
  private _umbHost: Parameters<UmbEntryPointOnInit>[0];
  private _intervalId: ReturnType<typeof setInterval> | null = null;

  constructor(host: Parameters<UmbEntryPointOnInit>[0]) {
    this._umbHost = host;
    host.consumeContext(UMB_MODAL_MANAGER_CONTEXT, (instance) => {
      this._host = instance;
    });
  }

  start() {
    this.stop();
    this._onNavigate = () => this._handleNavigation();
    window.addEventListener('popstate', this._onNavigate);
    document.addEventListener('click', this._onNavigate, { capture: true });
    this._handleNavigation();
  }

  stop() {
    if (this._intervalId !== null) {
      window.clearInterval(this._intervalId);
      this._intervalId = null;
    }
    if (this._onNavigate) {
      window.removeEventListener('popstate', this._onNavigate);
      document.removeEventListener('click', this._onNavigate, { capture: true });
      this._onNavigate = null;
    }
  }

  private _onNavigate: (() => void) | null = null;

  private _handleNavigation() {
    if (this._isLogViewerPage()) {
      if (this._intervalId === null) {
        this._intervalId = window.setInterval(() => this._tick(), 1000);
      }
    } else {
      if (this._intervalId !== null) {
        window.clearInterval(this._intervalId);
        this._intervalId = null;
        this._cachedMessagesList = null;
      }
    }
  }

  private _tick() {
    // Re-use cached element if it's still in the DOM
    if (this._cachedMessagesList && !this._cachedMessagesList.isConnected) {
      this._cachedMessagesList = null;
    }

    if (!this._cachedMessagesList) {
      this._cachedMessagesList = this._findElement(
        'umb-log-viewer-messages-list',
        document.body
      );
    }

    const messagesList = this._cachedMessagesList;
    if (!messagesList?.shadowRoot) return;

    this._enhanceHeader(messagesList.shadowRoot);
    this._enhanceMessages(messagesList.shadowRoot);
  }

  private _isLogViewerPage(): boolean {
    return window.location.pathname.includes('logviewer');
  }

  private _enhanceHeader(shadowRoot: ShadowRoot) {
    const header = shadowRoot.querySelector('#header');
    if (!header) return;
    if (header.querySelector('.log-ai-header')) return;

    const aiHeader = document.createElement('div');
    aiHeader.className = 'log-ai-header';
    aiHeader.textContent = 'AI';
    aiHeader.style.cssText = CELL_STYLE + ' font-weight: 600;';
    header.appendChild(aiHeader);
  }

  private _enhanceMessages(shadowRoot: ShadowRoot) {
    const main = shadowRoot.querySelector('#main');
    if (!main) return;

    const messages = main.querySelectorAll('umb-log-viewer-message');
    for (const msg of messages) {
      if (this._enhancedMessages.has(msg)) continue;
      if (!msg.shadowRoot) continue;

      const summary = msg.shadowRoot.querySelector('summary');
      if (!summary) continue;
      if (summary.querySelector('.log-ai-cell')) continue;

      this._enhancedMessages.add(msg);
      const logData = this._extractLogData(msg);

      const aiCell = document.createElement('div');
      aiCell.className = 'log-ai-cell';
      aiCell.style.cssText = CELL_STYLE;

      const button = document.createElement('button');
      button.type = 'button';
      button.title = 'Analyse with AI';
      button.setAttribute('aria-label', 'Analyse with AI');
      button.style.cssText =
        'cursor:pointer; background:none; border:none; padding:4px; display:flex; align-items:center; color:inherit;';
      button.innerHTML = AI_ICON_SVG;

      button.addEventListener('click', (e: Event) => {
        e.stopPropagation();
        e.preventDefault();
        this._showAiDialog(logData);
      });

      aiCell.appendChild(button);
      summary.appendChild(aiCell);
    }
  }

  private _extractLogData(msg: Element): LogData {
    const el = msg as unknown as {
      timestamp: string;
      level: string;
      renderedMessage: string;
      messageTemplate: string;
      exception: string;
      properties: Array<{ name: string; value: string }>;
    };

    const propsStr = el.properties
      ?.map((p) => `${p.name}: ${p.value}`)
      .join('\n');

    return {
      timestamp: el.timestamp || '',
      level: el.level || '',
      message: el.renderedMessage || '',
      messageTemplate: el.messageTemplate || undefined,
      exception: el.exception || undefined,
      properties: propsStr || undefined,
    };
  }

  private _showAiDialog(logData: LogData) {
    if (!this._host) {
      console.error('Log AI Summary: Modal manager not available');
      return;
    }

    this._host.open(this._umbHost, LOG_AI_SUMMARY_MODAL, {
      data: {
        level: logData.level,
        timestamp: logData.timestamp,
        message: logData.message,
        messageTemplate: logData.messageTemplate,
        exception: logData.exception,
        properties: logData.properties,
      },
    });
  }

  /**
   * Recursively searches through shadow DOMs to find an element by tag name.
   */
  private _findElement(
    tagName: string,
    root: ParentNode
  ): Element | null {
    const direct = root.querySelector(tagName);
    if (direct) return direct;

    const children = root.querySelectorAll('*');
    for (const child of children) {
      if (child.shadowRoot) {
        const found = this._findElement(tagName, child.shadowRoot);
        if (found) return found;
      }
    }
    return null;
  }
}
