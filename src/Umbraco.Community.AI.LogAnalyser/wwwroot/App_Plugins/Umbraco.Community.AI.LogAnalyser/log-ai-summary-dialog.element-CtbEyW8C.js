import { nothing as x, html as f, css as lt } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement as ht } from "@umbraco-cms/backoffice/modal";
import { UMB_AUTH_CONTEXT as ct } from "@umbraco-cms/backoffice/auth";
import { marked as st } from "@umbraco-cms/backoffice/external/marked";
import { DOMPurify as dt } from "@umbraco-cms/backoffice/external/dompurify";
const ut = (r) => (t, e) => {
  e !== void 0 ? e.addInitializer(() => {
    customElements.define(r, t);
  }) : customElements.define(r, t);
};
const O = globalThis, I = O.ShadowRoot && (O.ShadyCSS === void 0 || O.ShadyCSS.nativeShadow) && "adoptedStyleSheets" in Document.prototype && "replace" in CSSStyleSheet.prototype, it = /* @__PURE__ */ Symbol(), V = /* @__PURE__ */ new WeakMap();
let pt = class {
  constructor(t, e, s) {
    if (this._$cssResult$ = !0, s !== it) throw Error("CSSResult is not constructable. Use `unsafeCSS` or `css` instead.");
    this.cssText = t, this.t = e;
  }
  get styleSheet() {
    let t = this.o;
    const e = this.t;
    if (I && t === void 0) {
      const s = e !== void 0 && e.length === 1;
      s && (t = V.get(e)), t === void 0 && ((this.o = t = new CSSStyleSheet()).replaceSync(this.cssText), s && V.set(e, t));
    }
    return t;
  }
  toString() {
    return this.cssText;
  }
};
const ft = (r) => new pt(typeof r == "string" ? r : r + "", void 0, it), mt = (r, t) => {
  if (I) r.adoptedStyleSheets = t.map((e) => e instanceof CSSStyleSheet ? e : e.styleSheet);
  else for (const e of t) {
    const s = document.createElement("style"), i = O.litNonce;
    i !== void 0 && s.setAttribute("nonce", i), s.textContent = e.cssText, r.appendChild(s);
  }
}, J = I ? (r) => r : (r) => r instanceof CSSStyleSheet ? ((t) => {
  let e = "";
  for (const s of t.cssRules) e += s.cssText;
  return ft(e);
})(r) : r;
const { is: $t, defineProperty: _t, getOwnPropertyDescriptor: gt, getOwnPropertyNames: yt, getOwnPropertySymbols: vt, getPrototypeOf: At } = Object, N = globalThis, Z = N.trustedTypes, bt = Z ? Z.emptyScript : "", xt = N.reactiveElementPolyfillSupport, S = (r, t) => r, T = { toAttribute(r, t) {
  switch (t) {
    case Boolean:
      r = r ? bt : null;
      break;
    case Object:
    case Array:
      r = r == null ? r : JSON.stringify(r);
  }
  return r;
}, fromAttribute(r, t) {
  let e = r;
  switch (t) {
    case Boolean:
      e = r !== null;
      break;
    case Number:
      e = r === null ? null : Number(r);
      break;
    case Object:
    case Array:
      try {
        e = JSON.parse(r);
      } catch {
        e = null;
      }
  }
  return e;
} }, B = (r, t) => !$t(r, t), F = { attribute: !0, type: String, converter: T, reflect: !1, useDefault: !1, hasChanged: B };
Symbol.metadata ??= /* @__PURE__ */ Symbol("metadata"), N.litPropertyMetadata ??= /* @__PURE__ */ new WeakMap();
let w = class extends HTMLElement {
  static addInitializer(t) {
    this._$Ei(), (this.l ??= []).push(t);
  }
  static get observedAttributes() {
    return this.finalize(), this._$Eh && [...this._$Eh.keys()];
  }
  static createProperty(t, e = F) {
    if (e.state && (e.attribute = !1), this._$Ei(), this.prototype.hasOwnProperty(t) && ((e = Object.create(e)).wrapped = !0), this.elementProperties.set(t, e), !e.noAccessor) {
      const s = /* @__PURE__ */ Symbol(), i = this.getPropertyDescriptor(t, s, e);
      i !== void 0 && _t(this.prototype, t, i);
    }
  }
  static getPropertyDescriptor(t, e, s) {
    const { get: i, set: o } = gt(this.prototype, t) ?? { get() {
      return this[e];
    }, set(n) {
      this[e] = n;
    } };
    return { get: i, set(n) {
      const l = i?.call(this);
      o?.call(this, n), this.requestUpdate(t, l, s);
    }, configurable: !0, enumerable: !0 };
  }
  static getPropertyOptions(t) {
    return this.elementProperties.get(t) ?? F;
  }
  static _$Ei() {
    if (this.hasOwnProperty(S("elementProperties"))) return;
    const t = At(this);
    t.finalize(), t.l !== void 0 && (this.l = [...t.l]), this.elementProperties = new Map(t.elementProperties);
  }
  static finalize() {
    if (this.hasOwnProperty(S("finalized"))) return;
    if (this.finalized = !0, this._$Ei(), this.hasOwnProperty(S("properties"))) {
      const e = this.properties, s = [...yt(e), ...vt(e)];
      for (const i of s) this.createProperty(i, e[i]);
    }
    const t = this[Symbol.metadata];
    if (t !== null) {
      const e = litPropertyMetadata.get(t);
      if (e !== void 0) for (const [s, i] of e) this.elementProperties.set(s, i);
    }
    this._$Eh = /* @__PURE__ */ new Map();
    for (const [e, s] of this.elementProperties) {
      const i = this._$Eu(e, s);
      i !== void 0 && this._$Eh.set(i, e);
    }
    this.elementStyles = this.finalizeStyles(this.styles);
  }
  static finalizeStyles(t) {
    const e = [];
    if (Array.isArray(t)) {
      const s = new Set(t.flat(1 / 0).reverse());
      for (const i of s) e.unshift(J(i));
    } else t !== void 0 && e.push(J(t));
    return e;
  }
  static _$Eu(t, e) {
    const s = e.attribute;
    return s === !1 ? void 0 : typeof s == "string" ? s : typeof t == "string" ? t.toLowerCase() : void 0;
  }
  constructor() {
    super(), this._$Ep = void 0, this.isUpdatePending = !1, this.hasUpdated = !1, this._$Em = null, this._$Ev();
  }
  _$Ev() {
    this._$ES = new Promise((t) => this.enableUpdating = t), this._$AL = /* @__PURE__ */ new Map(), this._$E_(), this.requestUpdate(), this.constructor.l?.forEach((t) => t(this));
  }
  addController(t) {
    (this._$EO ??= /* @__PURE__ */ new Set()).add(t), this.renderRoot !== void 0 && this.isConnected && t.hostConnected?.();
  }
  removeController(t) {
    this._$EO?.delete(t);
  }
  _$E_() {
    const t = /* @__PURE__ */ new Map(), e = this.constructor.elementProperties;
    for (const s of e.keys()) this.hasOwnProperty(s) && (t.set(s, this[s]), delete this[s]);
    t.size > 0 && (this._$Ep = t);
  }
  createRenderRoot() {
    const t = this.shadowRoot ?? this.attachShadow(this.constructor.shadowRootOptions);
    return mt(t, this.constructor.elementStyles), t;
  }
  connectedCallback() {
    this.renderRoot ??= this.createRenderRoot(), this.enableUpdating(!0), this._$EO?.forEach((t) => t.hostConnected?.());
  }
  enableUpdating(t) {
  }
  disconnectedCallback() {
    this._$EO?.forEach((t) => t.hostDisconnected?.());
  }
  attributeChangedCallback(t, e, s) {
    this._$AK(t, s);
  }
  _$ET(t, e) {
    const s = this.constructor.elementProperties.get(t), i = this.constructor._$Eu(t, s);
    if (i !== void 0 && s.reflect === !0) {
      const o = (s.converter?.toAttribute !== void 0 ? s.converter : T).toAttribute(e, s.type);
      this._$Em = t, o == null ? this.removeAttribute(i) : this.setAttribute(i, o), this._$Em = null;
    }
  }
  _$AK(t, e) {
    const s = this.constructor, i = s._$Eh.get(t);
    if (i !== void 0 && this._$Em !== i) {
      const o = s.getPropertyOptions(i), n = typeof o.converter == "function" ? { fromAttribute: o.converter } : o.converter?.fromAttribute !== void 0 ? o.converter : T;
      this._$Em = i;
      const l = n.fromAttribute(e, o.type);
      this[i] = l ?? this._$Ej?.get(i) ?? l, this._$Em = null;
    }
  }
  requestUpdate(t, e, s, i = !1, o) {
    if (t !== void 0) {
      const n = this.constructor;
      if (i === !1 && (o = this[t]), s ??= n.getPropertyOptions(t), !((s.hasChanged ?? B)(o, e) || s.useDefault && s.reflect && o === this._$Ej?.get(t) && !this.hasAttribute(n._$Eu(t, s)))) return;
      this.C(t, e, s);
    }
    this.isUpdatePending === !1 && (this._$ES = this._$EP());
  }
  C(t, e, { useDefault: s, reflect: i, wrapped: o }, n) {
    s && !(this._$Ej ??= /* @__PURE__ */ new Map()).has(t) && (this._$Ej.set(t, n ?? e ?? this[t]), o !== !0 || n !== void 0) || (this._$AL.has(t) || (this.hasUpdated || s || (e = void 0), this._$AL.set(t, e)), i === !0 && this._$Em !== t && (this._$Eq ??= /* @__PURE__ */ new Set()).add(t));
  }
  async _$EP() {
    this.isUpdatePending = !0;
    try {
      await this._$ES;
    } catch (e) {
      Promise.reject(e);
    }
    const t = this.scheduleUpdate();
    return t != null && await t, !this.isUpdatePending;
  }
  scheduleUpdate() {
    return this.performUpdate();
  }
  performUpdate() {
    if (!this.isUpdatePending) return;
    if (!this.hasUpdated) {
      if (this.renderRoot ??= this.createRenderRoot(), this._$Ep) {
        for (const [i, o] of this._$Ep) this[i] = o;
        this._$Ep = void 0;
      }
      const s = this.constructor.elementProperties;
      if (s.size > 0) for (const [i, o] of s) {
        const { wrapped: n } = o, l = this[i];
        n !== !0 || this._$AL.has(i) || l === void 0 || this.C(i, void 0, o, l);
      }
    }
    let t = !1;
    const e = this._$AL;
    try {
      t = this.shouldUpdate(e), t ? (this.willUpdate(e), this._$EO?.forEach((s) => s.hostUpdate?.()), this.update(e)) : this._$EM();
    } catch (s) {
      throw t = !1, this._$EM(), s;
    }
    t && this._$AE(e);
  }
  willUpdate(t) {
  }
  _$AE(t) {
    this._$EO?.forEach((e) => e.hostUpdated?.()), this.hasUpdated || (this.hasUpdated = !0, this.firstUpdated(t)), this.updated(t);
  }
  _$EM() {
    this._$AL = /* @__PURE__ */ new Map(), this.isUpdatePending = !1;
  }
  get updateComplete() {
    return this.getUpdateComplete();
  }
  getUpdateComplete() {
    return this._$ES;
  }
  shouldUpdate(t) {
    return !0;
  }
  update(t) {
    this._$Eq &&= this._$Eq.forEach((e) => this._$ET(e, this[e])), this._$EM();
  }
  updated(t) {
  }
  firstUpdated(t) {
  }
};
w.elementStyles = [], w.shadowRootOptions = { mode: "open" }, w[S("elementProperties")] = /* @__PURE__ */ new Map(), w[S("finalized")] = /* @__PURE__ */ new Map(), xt?.({ ReactiveElement: w }), (N.reactiveElementVersions ??= []).push("2.1.2");
const wt = { attribute: !0, type: String, converter: T, reflect: !1, hasChanged: B }, Et = (r = wt, t, e) => {
  const { kind: s, metadata: i } = e;
  let o = globalThis.litPropertyMetadata.get(i);
  if (o === void 0 && globalThis.litPropertyMetadata.set(i, o = /* @__PURE__ */ new Map()), s === "setter" && ((r = Object.create(r)).wrapped = !0), o.set(e.name, r), s === "accessor") {
    const { name: n } = e;
    return { set(l) {
      const a = t.get.call(this);
      t.set.call(this, l), this.requestUpdate(n, a, r, !0, l);
    }, init(l) {
      return l !== void 0 && this.C(n, void 0, r, l), l;
    } };
  }
  if (s === "setter") {
    const { name: n } = e;
    return function(l) {
      const a = this[n];
      t.call(this, l), this.requestUpdate(n, a, r, !0, l);
    };
  }
  throw Error("Unsupported decorator location: " + s);
};
function St(r) {
  return (t, e) => typeof e == "object" ? Et(r, t, e) : ((s, i, o) => {
    const n = i.hasOwnProperty(o);
    return i.constructor.createProperty(o, s), n ? Object.getOwnPropertyDescriptor(i, o) : void 0;
  })(r, t, e);
}
function R(r) {
  return St({ ...r, state: !0, attribute: !1 });
}
const q = globalThis, K = (r) => r, M = q.trustedTypes, X = M ? M.createPolicy("lit-html", { createHTML: (r) => r }) : void 0, rt = "$lit$", $ = `lit$${Math.random().toFixed(9).slice(2)}$`, ot = "?" + $, Ct = `<${ot}>`, y = document, H = () => y.createComment(""), C = (r) => r === null || typeof r != "object" && typeof r != "function", W = Array.isArray, Pt = (r) => W(r) || typeof r?.[Symbol.iterator] == "function", L = `[ 	
\f\r]`, E = /<(?:(!--|\/[^a-zA-Z])|(\/?[a-zA-Z][^>\s]*)|(\/?$))/g, G = /-->/g, Q = />/g, _ = RegExp(`>|${L}(?:([^\\s"'>=/]+)(${L}*=${L}*(?:[^ 	
\f\r"'\`<>=]|("|')|))|$)`, "g"), Y = /'/g, tt = /"/g, nt = /^(?:script|style|textarea|title)$/i, A = /* @__PURE__ */ Symbol.for("lit-noChange"), c = /* @__PURE__ */ Symbol.for("lit-nothing"), et = /* @__PURE__ */ new WeakMap(), g = y.createTreeWalker(y, 129);
function at(r, t) {
  if (!W(r) || !r.hasOwnProperty("raw")) throw Error("invalid template strings array");
  return X !== void 0 ? X.createHTML(t) : t;
}
const kt = (r, t) => {
  const e = r.length - 1, s = [];
  let i, o = t === 2 ? "<svg>" : t === 3 ? "<math>" : "", n = E;
  for (let l = 0; l < e; l++) {
    const a = r[l];
    let d, u, h = -1, p = 0;
    for (; p < a.length && (n.lastIndex = p, u = n.exec(a), u !== null); ) p = n.lastIndex, n === E ? u[1] === "!--" ? n = G : u[1] !== void 0 ? n = Q : u[2] !== void 0 ? (nt.test(u[2]) && (i = RegExp("</" + u[2], "g")), n = _) : u[3] !== void 0 && (n = _) : n === _ ? u[0] === ">" ? (n = i ?? E, h = -1) : u[1] === void 0 ? h = -2 : (h = n.lastIndex - u[2].length, d = u[1], n = u[3] === void 0 ? _ : u[3] === '"' ? tt : Y) : n === tt || n === Y ? n = _ : n === G || n === Q ? n = E : (n = _, i = void 0);
    const m = n === _ && r[l + 1].startsWith("/>") ? " " : "";
    o += n === E ? a + Ct : h >= 0 ? (s.push(d), a.slice(0, h) + rt + a.slice(h) + $ + m) : a + $ + (h === -2 ? l : m);
  }
  return [at(r, o + (r[e] || "<?>") + (t === 2 ? "</svg>" : t === 3 ? "</math>" : "")), s];
};
class P {
  constructor({ strings: t, _$litType$: e }, s) {
    let i;
    this.parts = [];
    let o = 0, n = 0;
    const l = t.length - 1, a = this.parts, [d, u] = kt(t, e);
    if (this.el = P.createElement(d, s), g.currentNode = this.el.content, e === 2 || e === 3) {
      const h = this.el.content.firstChild;
      h.replaceWith(...h.childNodes);
    }
    for (; (i = g.nextNode()) !== null && a.length < l; ) {
      if (i.nodeType === 1) {
        if (i.hasAttributes()) for (const h of i.getAttributeNames()) if (h.endsWith(rt)) {
          const p = u[n++], m = i.getAttribute(h).split($), U = /([.?@])?(.*)/.exec(p);
          a.push({ type: 1, index: o, name: U[2], strings: m, ctor: U[1] === "." ? Ot : U[1] === "?" ? Tt : U[1] === "@" ? Mt : j }), i.removeAttribute(h);
        } else h.startsWith($) && (a.push({ type: 6, index: o }), i.removeAttribute(h));
        if (nt.test(i.tagName)) {
          const h = i.textContent.split($), p = h.length - 1;
          if (p > 0) {
            i.textContent = M ? M.emptyScript : "";
            for (let m = 0; m < p; m++) i.append(h[m], H()), g.nextNode(), a.push({ type: 2, index: ++o });
            i.append(h[p], H());
          }
        }
      } else if (i.nodeType === 8) if (i.data === ot) a.push({ type: 2, index: o });
      else {
        let h = -1;
        for (; (h = i.data.indexOf($, h + 1)) !== -1; ) a.push({ type: 7, index: o }), h += $.length - 1;
      }
      o++;
    }
  }
  static createElement(t, e) {
    const s = y.createElement("template");
    return s.innerHTML = t, s;
  }
}
function b(r, t, e = r, s) {
  if (t === A) return t;
  let i = s !== void 0 ? e._$Co?.[s] : e._$Cl;
  const o = C(t) ? void 0 : t._$litDirective$;
  return i?.constructor !== o && (i?._$AO?.(!1), o === void 0 ? i = void 0 : (i = new o(r), i._$AT(r, e, s)), s !== void 0 ? (e._$Co ??= [])[s] = i : e._$Cl = i), i !== void 0 && (t = b(r, i._$AS(r, t.values), i, s)), t;
}
class Ut {
  constructor(t, e) {
    this._$AV = [], this._$AN = void 0, this._$AD = t, this._$AM = e;
  }
  get parentNode() {
    return this._$AM.parentNode;
  }
  get _$AU() {
    return this._$AM._$AU;
  }
  u(t) {
    const { el: { content: e }, parts: s } = this._$AD, i = (t?.creationScope ?? y).importNode(e, !0);
    g.currentNode = i;
    let o = g.nextNode(), n = 0, l = 0, a = s[0];
    for (; a !== void 0; ) {
      if (n === a.index) {
        let d;
        a.type === 2 ? d = new z(o, o.nextSibling, this, t) : a.type === 1 ? d = new a.ctor(o, a.name, a.strings, this, t) : a.type === 6 && (d = new Ht(o, this, t)), this._$AV.push(d), a = s[++l];
      }
      n !== a?.index && (o = g.nextNode(), n++);
    }
    return g.currentNode = y, i;
  }
  p(t) {
    let e = 0;
    for (const s of this._$AV) s !== void 0 && (s.strings !== void 0 ? (s._$AI(t, s, e), e += s.strings.length - 2) : s._$AI(t[e])), e++;
  }
}
class z {
  get _$AU() {
    return this._$AM?._$AU ?? this._$Cv;
  }
  constructor(t, e, s, i) {
    this.type = 2, this._$AH = c, this._$AN = void 0, this._$AA = t, this._$AB = e, this._$AM = s, this.options = i, this._$Cv = i?.isConnected ?? !0;
  }
  get parentNode() {
    let t = this._$AA.parentNode;
    const e = this._$AM;
    return e !== void 0 && t?.nodeType === 11 && (t = e.parentNode), t;
  }
  get startNode() {
    return this._$AA;
  }
  get endNode() {
    return this._$AB;
  }
  _$AI(t, e = this) {
    t = b(this, t, e), C(t) ? t === c || t == null || t === "" ? (this._$AH !== c && this._$AR(), this._$AH = c) : t !== this._$AH && t !== A && this._(t) : t._$litType$ !== void 0 ? this.$(t) : t.nodeType !== void 0 ? this.T(t) : Pt(t) ? this.k(t) : this._(t);
  }
  O(t) {
    return this._$AA.parentNode.insertBefore(t, this._$AB);
  }
  T(t) {
    this._$AH !== t && (this._$AR(), this._$AH = this.O(t));
  }
  _(t) {
    this._$AH !== c && C(this._$AH) ? this._$AA.nextSibling.data = t : this.T(y.createTextNode(t)), this._$AH = t;
  }
  $(t) {
    const { values: e, _$litType$: s } = t, i = typeof s == "number" ? this._$AC(t) : (s.el === void 0 && (s.el = P.createElement(at(s.h, s.h[0]), this.options)), s);
    if (this._$AH?._$AD === i) this._$AH.p(e);
    else {
      const o = new Ut(i, this), n = o.u(this.options);
      o.p(e), this.T(n), this._$AH = o;
    }
  }
  _$AC(t) {
    let e = et.get(t.strings);
    return e === void 0 && et.set(t.strings, e = new P(t)), e;
  }
  k(t) {
    W(this._$AH) || (this._$AH = [], this._$AR());
    const e = this._$AH;
    let s, i = 0;
    for (const o of t) i === e.length ? e.push(s = new z(this.O(H()), this.O(H()), this, this.options)) : s = e[i], s._$AI(o), i++;
    i < e.length && (this._$AR(s && s._$AB.nextSibling, i), e.length = i);
  }
  _$AR(t = this._$AA.nextSibling, e) {
    for (this._$AP?.(!1, !0, e); t !== this._$AB; ) {
      const s = K(t).nextSibling;
      K(t).remove(), t = s;
    }
  }
  setConnected(t) {
    this._$AM === void 0 && (this._$Cv = t, this._$AP?.(t));
  }
}
class j {
  get tagName() {
    return this.element.tagName;
  }
  get _$AU() {
    return this._$AM._$AU;
  }
  constructor(t, e, s, i, o) {
    this.type = 1, this._$AH = c, this._$AN = void 0, this.element = t, this.name = e, this._$AM = i, this.options = o, s.length > 2 || s[0] !== "" || s[1] !== "" ? (this._$AH = Array(s.length - 1).fill(new String()), this.strings = s) : this._$AH = c;
  }
  _$AI(t, e = this, s, i) {
    const o = this.strings;
    let n = !1;
    if (o === void 0) t = b(this, t, e, 0), n = !C(t) || t !== this._$AH && t !== A, n && (this._$AH = t);
    else {
      const l = t;
      let a, d;
      for (t = o[0], a = 0; a < o.length - 1; a++) d = b(this, l[s + a], e, a), d === A && (d = this._$AH[a]), n ||= !C(d) || d !== this._$AH[a], d === c ? t = c : t !== c && (t += (d ?? "") + o[a + 1]), this._$AH[a] = d;
    }
    n && !i && this.j(t);
  }
  j(t) {
    t === c ? this.element.removeAttribute(this.name) : this.element.setAttribute(this.name, t ?? "");
  }
}
class Ot extends j {
  constructor() {
    super(...arguments), this.type = 3;
  }
  j(t) {
    this.element[this.name] = t === c ? void 0 : t;
  }
}
class Tt extends j {
  constructor() {
    super(...arguments), this.type = 4;
  }
  j(t) {
    this.element.toggleAttribute(this.name, !!t && t !== c);
  }
}
class Mt extends j {
  constructor(t, e, s, i, o) {
    super(t, e, s, i, o), this.type = 5;
  }
  _$AI(t, e = this) {
    if ((t = b(this, t, e, 0) ?? c) === A) return;
    const s = this._$AH, i = t === c && s !== c || t.capture !== s.capture || t.once !== s.once || t.passive !== s.passive, o = t !== c && (s === c || i);
    i && this.element.removeEventListener(this.name, this, s), o && this.element.addEventListener(this.name, this, t), this._$AH = t;
  }
  handleEvent(t) {
    typeof this._$AH == "function" ? this._$AH.call(this.options?.host ?? this.element, t) : this._$AH.handleEvent(t);
  }
}
class Ht {
  constructor(t, e, s) {
    this.element = t, this.type = 6, this._$AN = void 0, this._$AM = e, this.options = s;
  }
  get _$AU() {
    return this._$AM._$AU;
  }
  _$AI(t) {
    b(this, t);
  }
}
const Nt = q.litHtmlPolyfillSupport;
Nt?.(P, z), (q.litHtmlVersions ??= []).push("3.3.2");
const Rt = { CHILD: 2 }, zt = (r) => (...t) => ({ _$litDirective$: r, values: t });
class jt {
  constructor(t) {
  }
  get _$AU() {
    return this._$AM._$AU;
  }
  _$AT(t, e, s) {
    this._$Ct = t, this._$AM = e, this._$Ci = s;
  }
  _$AS(t, e) {
    return this.update(t, e);
  }
  update(t, e) {
    return this.render(...e);
  }
}
class D extends jt {
  constructor(t) {
    if (super(t), this.it = c, t.type !== Rt.CHILD) throw Error(this.constructor.directiveName + "() can only be used in child bindings");
  }
  render(t) {
    if (t === c || t == null) return this._t = void 0, this.it = t;
    if (t === A) return t;
    if (typeof t != "string") throw Error(this.constructor.directiveName + "() called with a non-string value");
    if (t === this.it) return this._t;
    this.it = t;
    const e = [t];
    return e.raw = e, this._t = { _$litType$: this.constructor.resultType, strings: e, values: [] };
  }
}
D.directiveName = "unsafeHTML", D.resultType = 1;
const Lt = zt(D);
var Dt = Object.defineProperty, It = Object.getOwnPropertyDescriptor, k = (r, t, e, s) => {
  for (var i = s > 1 ? void 0 : s ? It(t, e) : t, o = r.length - 1, n; o >= 0; o--)
    (n = r[o]) && (i = (s ? n(t, e, i) : n(i)) || i);
  return s && i && Dt(t, e, i), i;
};
function Bt(r) {
  switch (r.toLowerCase()) {
    case "error":
    case "fatal":
      return "var(--uui-color-danger, #d42054)";
    case "warning":
      return "var(--uui-color-warning, #f0ac00)";
    case "information":
    case "info":
      return "var(--uui-color-positive, #2bc37c)";
    case "debug":
    case "verbose":
      return "var(--uui-color-default, #68737d)";
    default:
      return "var(--uui-color-default, #68737d)";
  }
}
st.use({
  breaks: !0,
  renderer: {
    link({ href: r, text: t }) {
      const e = (r ?? "").replace(/&/g, "&amp;").replace(/"/g, "&quot;");
      return /^javascript:/i.test(e) ? t ?? "" : `<a href="${e}" target="_blank" rel="noopener">${t}</a>`;
    }
  }
});
let v = class extends ht {
  constructor() {
    super(...arguments), this._summary = "", this._loading = !0, this._error = "", this._snarky = !1, this._fetchSummary = async (r) => {
      this._abortController?.abort(), this._abortController = new AbortController(), this._loading = !0, this._error = "";
      try {
        const t = await this.getContext(ct);
        if (!t) {
          this._error = "Authentication context not available.", this._loading = !1;
          return;
        }
        const e = await t.getLatestToken(), s = await fetch(
          "/umbraco/ailoganalyser/api/v1.0/analyse",
          {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
              Authorization: `Bearer ${e}`
            },
            body: JSON.stringify({
              level: this.data?.level,
              timestamp: this.data?.timestamp,
              message: this.data?.message,
              messageTemplate: this.data?.messageTemplate || void 0,
              exception: this.data?.exception || void 0,
              properties: this.data?.properties || void 0,
              // Omitted when undefined so the backend falls back to its configured default.
              snarky: r
            }),
            signal: this._abortController.signal
          }
        );
        if (!s.ok) {
          const o = await s.text();
          throw new Error(
            `API returned ${s.status}: ${o.substring(0, 200)}`
          );
        }
        const i = await s.json();
        this._summary = i.summary ?? "", this._snarky = i.snarky ?? !1;
      } catch (t) {
        if (t instanceof DOMException && t.name === "AbortError") return;
        this._error = t instanceof Error ? t.message : "Failed to get AI summary.";
      } finally {
        this._loading = !1;
      }
    };
  }
  connectedCallback() {
    super.connectedCallback(), this._fetchSummary();
  }
  disconnectedCallback() {
    super.disconnectedCallback(), this._abortController?.abort();
  }
  _close() {
    this.modalContext?.reject();
  }
  render() {
    const r = Bt(this.data?.level ?? "");
    return f`
      <uui-dialog-layout headline="AI Log Analysis">
        <div class="content-wrapper">
          ${this._snarky ? f`
                <div class="snarky-banner">
                  <uui-icon name="icon-alert"></uui-icon>
                  <span
                    >Snarky mode is on — the AI is being deliberately cheeky and
                    condescending. The technical details are still accurate.</span
                  >
                </div>
              ` : x}
          <div class="log-section">
            <div class="log-meta">
          ${this.data?.level ? f`<span
                class="level-badge"
                style="background:${r}"
                >${this.data.level}</span
              >` : x}
          ${this.data?.timestamp ? f`<span>${this.data.timestamp}</span>` : x}
        </div>

        <div class="log-message">${this.data?.message}</div>
          </div>

          <hr class="divider" />

          <div class="summary-section">
            <div class="summary-label">AI Summary</div>

        ${this._loading ? f`
              <div class="loading">
                <uui-loader-bar></uui-loader-bar>
                <span class="loading-text"
                  >Analysing log entry with AI...</span
                >
              </div>
            ` : this._error ? f`<div class="error">${this._error}</div>` : f`
                <div class="summary-content">
                  ${Lt(dt.sanitize(String(st.parse(this._summary, { async: !1 }))))}
                    </div>
                  `}
          </div>
        </div>

        ${!this._loading && !this._error && this._snarky ? f`
              <uui-button
                slot="actions"
                look="secondary"
                label="Re-analyse without snark"
                @click=${() => this._fetchSummary(!1)}
              >
                <uui-icon name="icon-wand"></uui-icon>
                Re-analyse without snark
              </uui-button>
            ` : x}
        ${!this._loading && !this._error ? f`
              <uui-button
                slot="actions"
                look="secondary"
                label="Re-analyse"
                @click=${() => this._fetchSummary()}
              >
                <uui-icon name="icon-refresh"></uui-icon>
                Re-analyse
              </uui-button>
            ` : x}
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
};
v.styles = lt`
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

    .snarky-banner {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 8px 12px;
      margin-bottom: 16px;
      background: var(--uui-color-warning, #f0ac00);
      color: var(--uui-color-warning-contrast, #000);
      border-radius: 4px;
      font-size: 13px;
      font-weight: 600;
    }

    .snarky-banner uui-icon {
      flex-shrink: 0;
    }
  `;
k([
  R()
], v.prototype, "_summary", 2);
k([
  R()
], v.prototype, "_loading", 2);
k([
  R()
], v.prototype, "_error", 2);
k([
  R()
], v.prototype, "_snarky", 2);
v = k([
  ut("log-ai-summary-dialog")
], v);
export {
  v as default
};
//# sourceMappingURL=log-ai-summary-dialog.element-CtbEyW8C.js.map
