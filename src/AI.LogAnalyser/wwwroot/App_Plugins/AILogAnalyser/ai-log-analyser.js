import { UmbModalToken as d, UMB_MODAL_MANAGER_CONTEXT as m } from "@umbraco-cms/backoffice/modal";
const h = new d(
  "LogAiSummary.Modal",
  {
    modal: {
      type: "dialog",
      size: "small"
    }
  }
), p = (l, e) => {
  console.log("Log AI Summary: Entry point initialized");
  const t = {
    type: "modal",
    alias: "LogAiSummary.Modal",
    name: "Log AI Summary Modal",
    js: () => import("./log-ai-summary-dialog.element-CgLC94cs.js")
  };
  e.register(t), new u(l).start();
};
class u {
  constructor(e) {
    this._enhancedMessages = /* @__PURE__ */ new WeakSet(), this._lastHeaderElement = null, this._umbHost = e, e.consumeContext(m, (t) => {
      this._host = t;
    });
  }
  start() {
    window.setInterval(() => this._tick(), 1e3);
  }
  _tick() {
    if (!this._isLogViewerPage()) return;
    const e = this._findElement(
      "umb-log-viewer-messages-list",
      document.body
    );
    e?.shadowRoot && (this._enhanceHeader(e.shadowRoot), this._enhanceMessages(e.shadowRoot));
  }
  _isLogViewerPage() {
    return window.location.pathname.includes("logviewer");
  }
  _enhanceHeader(e) {
    const t = e.querySelector("#header");
    if (!t || (this._lastHeaderElement !== t && (this._lastHeaderElement = t), t.querySelector(".log-ai-header"))) return;
    const o = document.createElement("div");
    o.className = "log-ai-header", o.textContent = "AI", o.style.cssText = "flex: 0 0 4ch; box-sizing: border-box; padding: 10px 20px; display: flex; align-items: center; justify-content: center; font-weight: 600; margin-left: auto;", t.appendChild(o), console.log("Log AI Summary: Header column added");
  }
  _enhanceMessages(e) {
    const t = e.querySelector("#main");
    if (!t) return;
    const o = t.querySelectorAll("umb-log-viewer-message");
    for (const n of o) {
      if (this._enhancedMessages.has(n) || !n.shadowRoot) continue;
      const i = n.shadowRoot.querySelector("summary");
      if (!i || i.querySelector(".log-ai-cell")) continue;
      this._enhancedMessages.add(n);
      const a = this._extractLogData(n), r = document.createElement("div");
      r.className = "log-ai-cell", r.style.cssText = "flex: 0 0 4ch; box-sizing: border-box; padding: 10px 20px; display: flex; align-items: center; justify-content: center; margin-left: auto;";
      const s = document.createElement("button");
      s.type = "button", s.title = "Analyse with AI", s.setAttribute("aria-label", "Analyse with AI"), s.style.cssText = "cursor:pointer; background:none; border:none; padding:4px; display:flex; align-items:center; color:inherit;", s.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.75" viewBox="0 0 24 24"><path d="m21.64 3.64-1.28-1.28a1.21 1.21 0 0 0-1.72 0L2.36 18.64a1.21 1.21 0 0 0 0 1.72l1.28 1.28a1.2 1.2 0 0 0 1.72 0L21.64 5.36a1.2 1.2 0 0 0 0-1.72M14 7l3 3M5 6v4M19 14v4M10 2v2M7 8H3M21 16h-4M11 3H9"/></svg>', s.addEventListener("click", (c) => {
        c.stopPropagation(), c.preventDefault(), this._showAiDialog(a);
      }), r.appendChild(s), i.appendChild(r);
    }
  }
  _extractLogData(e) {
    const t = e, o = t.properties?.map((n) => `${n.name}: ${n.value}`).join(`
`);
    return {
      timestamp: t.timestamp || "",
      level: t.level || "",
      message: t.renderedMessage || "",
      exception: t.exception || void 0,
      properties: o || void 0
    };
  }
  _showAiDialog(e) {
    if (!this._host) {
      console.error("Log AI Summary: Modal manager not available");
      return;
    }
    this._host.open(this._umbHost, h, {
      data: {
        level: e.level,
        timestamp: e.timestamp,
        message: e.message,
        exception: e.exception,
        properties: e.properties
      }
    });
  }
  /**
   * Recursively searches through shadow DOMs to find an element by tag name.
   */
  _findElement(e, t) {
    const o = t.querySelector(e);
    if (o) return o;
    const n = t.querySelectorAll("*");
    for (const i of n)
      if (i.shadowRoot) {
        const a = this._findElement(e, i.shadowRoot);
        if (a) return a;
      }
    return null;
  }
}
export {
  p as onInit
};
//# sourceMappingURL=ai-log-analyser.js.map
