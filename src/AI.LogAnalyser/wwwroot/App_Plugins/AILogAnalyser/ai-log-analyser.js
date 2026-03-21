import { UmbModalToken as h, UMB_MODAL_MANAGER_CONTEXT as m } from "@umbraco-cms/backoffice/modal";
const u = new h(
  "LogAiSummary.Modal",
  {
    modal: {
      type: "dialog",
      size: "medium"
    }
  }
), f = (l, e) => {
  const t = {
    type: "modal",
    alias: "LogAiSummary.Modal",
    name: "Log AI Summary Modal",
    js: () => import("./log-ai-summary-dialog.element-CbouTqKD.js")
  };
  e.register(t), new p(l).start();
}, d = "flex: 0 0 4ch; box-sizing: border-box; padding: 10px 20px; display: flex; align-items: center; justify-content: center; margin-left: auto;", g = '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.75" viewBox="0 0 24 24"><path d="m21.64 3.64-1.28-1.28a1.21 1.21 0 0 0-1.72 0L2.36 18.64a1.21 1.21 0 0 0 0 1.72l1.28 1.28a1.2 1.2 0 0 0 1.72 0L21.64 5.36a1.2 1.2 0 0 0 0-1.72M14 7l3 3M5 6v4M19 14v4M10 2v2M7 8H3M21 16h-4M11 3H9"/></svg>';
class p {
  constructor(e) {
    this._enhancedMessages = /* @__PURE__ */ new WeakSet(), this._cachedMessagesList = null, this._intervalId = null, this._umbHost = e, e.consumeContext(m, (t) => {
      this._host = t;
    });
  }
  start() {
    this.stop(), this._intervalId = window.setInterval(() => this._tick(), 1e3);
  }
  stop() {
    this._intervalId !== null && (window.clearInterval(this._intervalId), this._intervalId = null);
  }
  _tick() {
    if (!this._isLogViewerPage()) return;
    this._cachedMessagesList && !this._cachedMessagesList.isConnected && (this._cachedMessagesList = null), this._cachedMessagesList || (this._cachedMessagesList = this._findElement(
      "umb-log-viewer-messages-list",
      document.body
    ));
    const e = this._cachedMessagesList;
    e?.shadowRoot && (this._enhanceHeader(e.shadowRoot), this._enhanceMessages(e.shadowRoot));
  }
  _isLogViewerPage() {
    return window.location.pathname.includes("logviewer");
  }
  _enhanceHeader(e) {
    const t = e.querySelector("#header");
    if (!t || t.querySelector(".log-ai-header")) return;
    const s = document.createElement("div");
    s.className = "log-ai-header", s.textContent = "AI", s.style.cssText = d + " font-weight: 600;", t.appendChild(s);
  }
  _enhanceMessages(e) {
    const t = e.querySelector("#main");
    if (!t) return;
    const s = t.querySelectorAll("umb-log-viewer-message");
    for (const n of s) {
      if (this._enhancedMessages.has(n) || !n.shadowRoot) continue;
      const o = n.shadowRoot.querySelector("summary");
      if (!o || o.querySelector(".log-ai-cell")) continue;
      this._enhancedMessages.add(n);
      const a = this._extractLogData(n), r = document.createElement("div");
      r.className = "log-ai-cell", r.style.cssText = d;
      const i = document.createElement("button");
      i.type = "button", i.title = "Analyse with AI", i.setAttribute("aria-label", "Analyse with AI"), i.style.cssText = "cursor:pointer; background:none; border:none; padding:4px; display:flex; align-items:center; color:inherit;", i.innerHTML = g, i.addEventListener("click", (c) => {
        c.stopPropagation(), c.preventDefault(), this._showAiDialog(a);
      }), r.appendChild(i), o.appendChild(r);
    }
  }
  _extractLogData(e) {
    const t = e, s = t.properties?.map((n) => `${n.name}: ${n.value}`).join(`
`);
    return {
      timestamp: t.timestamp || "",
      level: t.level || "",
      message: t.renderedMessage || "",
      messageTemplate: t.messageTemplate || void 0,
      exception: t.exception || void 0,
      properties: s || void 0
    };
  }
  _showAiDialog(e) {
    if (!this._host) {
      console.error("Log AI Summary: Modal manager not available");
      return;
    }
    this._host.open(this._umbHost, u, {
      data: {
        level: e.level,
        timestamp: e.timestamp,
        message: e.message,
        messageTemplate: e.messageTemplate,
        exception: e.exception,
        properties: e.properties
      }
    });
  }
  /**
   * Recursively searches through shadow DOMs to find an element by tag name.
   */
  _findElement(e, t) {
    const s = t.querySelector(e);
    if (s) return s;
    const n = t.querySelectorAll("*");
    for (const o of n)
      if (o.shadowRoot) {
        const a = this._findElement(e, o.shadowRoot);
        if (a) return a;
      }
    return null;
  }
}
export {
  f as onInit
};
//# sourceMappingURL=ai-log-analyser.js.map
