import { nothing as z, html as f, css as at } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement as lt } from "@umbraco-cms/backoffice/modal";
import { UMB_AUTH_CONTEXT as ht } from "@umbraco-cms/backoffice/auth";
const ct = (r) => (t, e) => {
  e !== void 0 ? e.addInitializer(() => {
    customElements.define(r, t);
  }) : customElements.define(r, t);
};
const U = globalThis, I = U.ShadowRoot && (U.ShadyCSS === void 0 || U.ShadyCSS.nativeShadow) && "adoptedStyleSheets" in Document.prototype && "replace" in CSSStyleSheet.prototype, st = /* @__PURE__ */ Symbol(), V = /* @__PURE__ */ new WeakMap();
let dt = class {
  constructor(t, e, s) {
    if (this._$cssResult$ = !0, s !== st) throw Error("CSSResult is not constructable. Use `unsafeCSS` or `css` instead.");
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
const pt = (r) => new dt(typeof r == "string" ? r : r + "", void 0, st), ut = (r, t) => {
  if (I) r.adoptedStyleSheets = t.map((e) => e instanceof CSSStyleSheet ? e : e.styleSheet);
  else for (const e of t) {
    const s = document.createElement("style"), i = U.litNonce;
    i !== void 0 && s.setAttribute("nonce", i), s.textContent = e.cssText, r.appendChild(s);
  }
}, J = I ? (r) => r : (r) => r instanceof CSSStyleSheet ? ((t) => {
  let e = "";
  for (const s of t.cssRules) e += s.cssText;
  return pt(e);
})(r) : r;
const { is: $t, defineProperty: mt, getOwnPropertyDescriptor: ft, getOwnPropertyNames: gt, getOwnPropertySymbols: _t, getPrototypeOf: yt } = Object, H = globalThis, Z = H.trustedTypes, vt = Z ? Z.emptyScript : "", At = H.reactiveElementPolyfillSupport, w = (r, t) => r, O = { toAttribute(r, t) {
  switch (t) {
    case Boolean:
      r = r ? vt : null;
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
} }, D = (r, t) => !$t(r, t), F = { attribute: !0, type: String, converter: O, reflect: !1, useDefault: !1, hasChanged: D };
Symbol.metadata ??= /* @__PURE__ */ Symbol("metadata"), H.litPropertyMetadata ??= /* @__PURE__ */ new WeakMap();
let x = class extends HTMLElement {
  static addInitializer(t) {
    this._$Ei(), (this.l ??= []).push(t);
  }
  static get observedAttributes() {
    return this.finalize(), this._$Eh && [...this._$Eh.keys()];
  }
  static createProperty(t, e = F) {
    if (e.state && (e.attribute = !1), this._$Ei(), this.prototype.hasOwnProperty(t) && ((e = Object.create(e)).wrapped = !0), this.elementProperties.set(t, e), !e.noAccessor) {
      const s = /* @__PURE__ */ Symbol(), i = this.getPropertyDescriptor(t, s, e);
      i !== void 0 && mt(this.prototype, t, i);
    }
  }
  static getPropertyDescriptor(t, e, s) {
    const { get: i, set: o } = ft(this.prototype, t) ?? { get() {
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
    if (this.hasOwnProperty(w("elementProperties"))) return;
    const t = yt(this);
    t.finalize(), t.l !== void 0 && (this.l = [...t.l]), this.elementProperties = new Map(t.elementProperties);
  }
  static finalize() {
    if (this.hasOwnProperty(w("finalized"))) return;
    if (this.finalized = !0, this._$Ei(), this.hasOwnProperty(w("properties"))) {
      const e = this.properties, s = [...gt(e), ..._t(e)];
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
    return ut(t, this.constructor.elementStyles), t;
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
      const o = (s.converter?.toAttribute !== void 0 ? s.converter : O).toAttribute(e, s.type);
      this._$Em = t, o == null ? this.removeAttribute(i) : this.setAttribute(i, o), this._$Em = null;
    }
  }
  _$AK(t, e) {
    const s = this.constructor, i = s._$Eh.get(t);
    if (i !== void 0 && this._$Em !== i) {
      const o = s.getPropertyOptions(i), n = typeof o.converter == "function" ? { fromAttribute: o.converter } : o.converter?.fromAttribute !== void 0 ? o.converter : O;
      this._$Em = i;
      const l = n.fromAttribute(e, o.type);
      this[i] = l ?? this._$Ej?.get(i) ?? l, this._$Em = null;
    }
  }
  requestUpdate(t, e, s, i = !1, o) {
    if (t !== void 0) {
      const n = this.constructor;
      if (i === !1 && (o = this[t]), s ??= n.getPropertyOptions(t), !((s.hasChanged ?? D)(o, e) || s.useDefault && s.reflect && o === this._$Ej?.get(t) && !this.hasAttribute(n._$Eu(t, s)))) return;
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
x.elementStyles = [], x.shadowRootOptions = { mode: "open" }, x[w("elementProperties")] = /* @__PURE__ */ new Map(), x[w("finalized")] = /* @__PURE__ */ new Map(), At?.({ ReactiveElement: x }), (H.reactiveElementVersions ??= []).push("2.1.2");
const bt = { attribute: !0, type: String, converter: O, reflect: !1, hasChanged: D }, xt = (r = bt, t, e) => {
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
function Et(r) {
  return (t, e) => typeof e == "object" ? xt(r, t, e) : ((s, i, o) => {
    const n = i.hasOwnProperty(o);
    return i.constructor.createProperty(o, s), n ? Object.getOwnPropertyDescriptor(i, o) : void 0;
  })(r, t, e);
}
function B(r) {
  return Et({ ...r, state: !0, attribute: !1 });
}
const W = globalThis, K = (r) => r, T = W.trustedTypes, X = T ? T.createPolicy("lit-html", { createHTML: (r) => r }) : void 0, it = "$lit$", m = `lit$${Math.random().toFixed(9).slice(2)}$`, rt = "?" + m, wt = `<${rt}>`, y = document, M = () => y.createComment(""), S = (r) => r === null || typeof r != "object" && typeof r != "function", q = Array.isArray, St = (r) => q(r) || typeof r?.[Symbol.iterator] == "function", j = `[ 	
\f\r]`, E = /<(?:(!--|\/[^a-zA-Z])|(\/?[a-zA-Z][^>\s]*)|(\/?$))/g, G = /-->/g, Q = />/g, g = RegExp(`>|${j}(?:([^\\s"'>=/]+)(${j}*=${j}*(?:[^ 	
\f\r"'\`<>=]|("|')|))|$)`, "g"), Y = /'/g, tt = /"/g, ot = /^(?:script|style|textarea|title)$/i, v = /* @__PURE__ */ Symbol.for("lit-noChange"), c = /* @__PURE__ */ Symbol.for("lit-nothing"), et = /* @__PURE__ */ new WeakMap(), _ = y.createTreeWalker(y, 129);
function nt(r, t) {
  if (!q(r) || !r.hasOwnProperty("raw")) throw Error("invalid template strings array");
  return X !== void 0 ? X.createHTML(t) : t;
}
const Ct = (r, t) => {
  const e = r.length - 1, s = [];
  let i, o = t === 2 ? "<svg>" : t === 3 ? "<math>" : "", n = E;
  for (let l = 0; l < e; l++) {
    const a = r[l];
    let d, p, h = -1, u = 0;
    for (; u < a.length && (n.lastIndex = u, p = n.exec(a), p !== null); ) u = n.lastIndex, n === E ? p[1] === "!--" ? n = G : p[1] !== void 0 ? n = Q : p[2] !== void 0 ? (ot.test(p[2]) && (i = RegExp("</" + p[2], "g")), n = g) : p[3] !== void 0 && (n = g) : n === g ? p[0] === ">" ? (n = i ?? E, h = -1) : p[1] === void 0 ? h = -2 : (h = n.lastIndex - p[2].length, d = p[1], n = p[3] === void 0 ? g : p[3] === '"' ? tt : Y) : n === tt || n === Y ? n = g : n === G || n === Q ? n = E : (n = g, i = void 0);
    const $ = n === g && r[l + 1].startsWith("/>") ? " " : "";
    o += n === E ? a + wt : h >= 0 ? (s.push(d), a.slice(0, h) + it + a.slice(h) + m + $) : a + m + (h === -2 ? l : $);
  }
  return [nt(r, o + (r[e] || "<?>") + (t === 2 ? "</svg>" : t === 3 ? "</math>" : "")), s];
};
class C {
  constructor({ strings: t, _$litType$: e }, s) {
    let i;
    this.parts = [];
    let o = 0, n = 0;
    const l = t.length - 1, a = this.parts, [d, p] = Ct(t, e);
    if (this.el = C.createElement(d, s), _.currentNode = this.el.content, e === 2 || e === 3) {
      const h = this.el.content.firstChild;
      h.replaceWith(...h.childNodes);
    }
    for (; (i = _.nextNode()) !== null && a.length < l; ) {
      if (i.nodeType === 1) {
        if (i.hasAttributes()) for (const h of i.getAttributeNames()) if (h.endsWith(it)) {
          const u = p[n++], $ = i.getAttribute(h).split(m), P = /([.?@])?(.*)/.exec(u);
          a.push({ type: 1, index: o, name: P[2], strings: $, ctor: P[1] === "." ? Ut : P[1] === "?" ? Ot : P[1] === "@" ? Tt : k }), i.removeAttribute(h);
        } else h.startsWith(m) && (a.push({ type: 6, index: o }), i.removeAttribute(h));
        if (ot.test(i.tagName)) {
          const h = i.textContent.split(m), u = h.length - 1;
          if (u > 0) {
            i.textContent = T ? T.emptyScript : "";
            for (let $ = 0; $ < u; $++) i.append(h[$], M()), _.nextNode(), a.push({ type: 2, index: ++o });
            i.append(h[u], M());
          }
        }
      } else if (i.nodeType === 8) if (i.data === rt) a.push({ type: 2, index: o });
      else {
        let h = -1;
        for (; (h = i.data.indexOf(m, h + 1)) !== -1; ) a.push({ type: 7, index: o }), h += m.length - 1;
      }
      o++;
    }
  }
  static createElement(t, e) {
    const s = y.createElement("template");
    return s.innerHTML = t, s;
  }
}
function A(r, t, e = r, s) {
  if (t === v) return t;
  let i = s !== void 0 ? e._$Co?.[s] : e._$Cl;
  const o = S(t) ? void 0 : t._$litDirective$;
  return i?.constructor !== o && (i?._$AO?.(!1), o === void 0 ? i = void 0 : (i = new o(r), i._$AT(r, e, s)), s !== void 0 ? (e._$Co ??= [])[s] = i : e._$Cl = i), i !== void 0 && (t = A(r, i._$AS(r, t.values), i, s)), t;
}
class Pt {
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
    _.currentNode = i;
    let o = _.nextNode(), n = 0, l = 0, a = s[0];
    for (; a !== void 0; ) {
      if (n === a.index) {
        let d;
        a.type === 2 ? d = new N(o, o.nextSibling, this, t) : a.type === 1 ? d = new a.ctor(o, a.name, a.strings, this, t) : a.type === 6 && (d = new Mt(o, this, t)), this._$AV.push(d), a = s[++l];
      }
      n !== a?.index && (o = _.nextNode(), n++);
    }
    return _.currentNode = y, i;
  }
  p(t) {
    let e = 0;
    for (const s of this._$AV) s !== void 0 && (s.strings !== void 0 ? (s._$AI(t, s, e), e += s.strings.length - 2) : s._$AI(t[e])), e++;
  }
}
class N {
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
    t = A(this, t, e), S(t) ? t === c || t == null || t === "" ? (this._$AH !== c && this._$AR(), this._$AH = c) : t !== this._$AH && t !== v && this._(t) : t._$litType$ !== void 0 ? this.$(t) : t.nodeType !== void 0 ? this.T(t) : St(t) ? this.k(t) : this._(t);
  }
  O(t) {
    return this._$AA.parentNode.insertBefore(t, this._$AB);
  }
  T(t) {
    this._$AH !== t && (this._$AR(), this._$AH = this.O(t));
  }
  _(t) {
    this._$AH !== c && S(this._$AH) ? this._$AA.nextSibling.data = t : this.T(y.createTextNode(t)), this._$AH = t;
  }
  $(t) {
    const { values: e, _$litType$: s } = t, i = typeof s == "number" ? this._$AC(t) : (s.el === void 0 && (s.el = C.createElement(nt(s.h, s.h[0]), this.options)), s);
    if (this._$AH?._$AD === i) this._$AH.p(e);
    else {
      const o = new Pt(i, this), n = o.u(this.options);
      o.p(e), this.T(n), this._$AH = o;
    }
  }
  _$AC(t) {
    let e = et.get(t.strings);
    return e === void 0 && et.set(t.strings, e = new C(t)), e;
  }
  k(t) {
    q(this._$AH) || (this._$AH = [], this._$AR());
    const e = this._$AH;
    let s, i = 0;
    for (const o of t) i === e.length ? e.push(s = new N(this.O(M()), this.O(M()), this, this.options)) : s = e[i], s._$AI(o), i++;
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
class k {
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
    if (o === void 0) t = A(this, t, e, 0), n = !S(t) || t !== this._$AH && t !== v, n && (this._$AH = t);
    else {
      const l = t;
      let a, d;
      for (t = o[0], a = 0; a < o.length - 1; a++) d = A(this, l[s + a], e, a), d === v && (d = this._$AH[a]), n ||= !S(d) || d !== this._$AH[a], d === c ? t = c : t !== c && (t += (d ?? "") + o[a + 1]), this._$AH[a] = d;
    }
    n && !i && this.j(t);
  }
  j(t) {
    t === c ? this.element.removeAttribute(this.name) : this.element.setAttribute(this.name, t ?? "");
  }
}
class Ut extends k {
  constructor() {
    super(...arguments), this.type = 3;
  }
  j(t) {
    this.element[this.name] = t === c ? void 0 : t;
  }
}
class Ot extends k {
  constructor() {
    super(...arguments), this.type = 4;
  }
  j(t) {
    this.element.toggleAttribute(this.name, !!t && t !== c);
  }
}
class Tt extends k {
  constructor(t, e, s, i, o) {
    super(t, e, s, i, o), this.type = 5;
  }
  _$AI(t, e = this) {
    if ((t = A(this, t, e, 0) ?? c) === v) return;
    const s = this._$AH, i = t === c && s !== c || t.capture !== s.capture || t.once !== s.once || t.passive !== s.passive, o = t !== c && (s === c || i);
    i && this.element.removeEventListener(this.name, this, s), o && this.element.addEventListener(this.name, this, t), this._$AH = t;
  }
  handleEvent(t) {
    typeof this._$AH == "function" ? this._$AH.call(this.options?.host ?? this.element, t) : this._$AH.handleEvent(t);
  }
}
class Mt {
  constructor(t, e, s) {
    this.element = t, this.type = 6, this._$AN = void 0, this._$AM = e, this.options = s;
  }
  get _$AU() {
    return this._$AM._$AU;
  }
  _$AI(t) {
    A(this, t);
  }
}
const Ht = W.litHtmlPolyfillSupport;
Ht?.(C, N), (W.litHtmlVersions ??= []).push("3.3.2");
const Nt = { CHILD: 2 }, kt = (r) => (...t) => ({ _$litDirective$: r, values: t });
class Rt {
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
class L extends Rt {
  constructor(t) {
    if (super(t), this.it = c, t.type !== Nt.CHILD) throw Error(this.constructor.directiveName + "() can only be used in child bindings");
  }
  render(t) {
    if (t === c || t == null) return this._t = void 0, this.it = t;
    if (t === v) return t;
    if (typeof t != "string") throw Error(this.constructor.directiveName + "() called with a non-string value");
    if (t === this.it) return this._t;
    this.it = t;
    const e = [t];
    return e.raw = e, this._t = { _$litType$: this.constructor.resultType, strings: e, values: [] };
  }
}
L.directiveName = "unsafeHTML", L.resultType = 1;
const zt = kt(L);
var jt = Object.defineProperty, Lt = Object.getOwnPropertyDescriptor, R = (r, t, e, s) => {
  for (var i = s > 1 ? void 0 : s ? Lt(t, e) : t, o = r.length - 1, n; o >= 0; o--)
    (n = r[o]) && (i = (s ? n(t, e, i) : n(i)) || i);
  return s && i && jt(t, e, i), i;
};
function It(r) {
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
function Dt(r) {
  return r.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/^### (.+)$/gm, "<h4>$1</h4>").replace(/^## (.+)$/gm, "<h3>$1</h3>").replace(/^# (.+)$/gm, "<h2>$1</h2>").replace(/\*\*(.+?)\*\*/g, "<strong>$1</strong>").replace(/\*(.+?)\*/g, "<em>$1</em>").replace(/`(.+?)`/g, "<code>$1</code>").replace(/^- (.+)$/gm, "<li>$1</li>").replace(/(<li>.*<\/li>)/gs, "<ul>$1</ul>").replace(/<\/ul>\s*<ul>/g, "").replace(/\n{2,}/g, "</p><p>").replace(/\n/g, "<br>").replace(/^(.+)$/, "<p>$1</p>");
}
let b = class extends lt {
  constructor() {
    super(...arguments), this._summary = "", this._loading = !0, this._error = "";
  }
  connectedCallback() {
    super.connectedCallback();
    const r = this.closest("uui-dialog");
    r && (r.style.maxWidth = "800px"), this._fetchSummary();
  }
  async _fetchSummary() {
    this._loading = !0, this._error = "";
    try {
      const t = await (await this.getContext(ht)).getLatestToken(), e = await fetch(
        "/umbraco/ailoganalyser/api/v1.0/analyse",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${t}`
          },
          body: JSON.stringify({
            level: this.data?.level,
            timestamp: this.data?.timestamp,
            message: this.data?.message,
            exception: this.data?.exception || void 0,
            properties: this.data?.properties || void 0
          })
        }
      );
      if (!e.ok) {
        const i = await e.text();
        throw new Error(
          `API returned ${e.status}: ${i.substring(0, 200)}`
        );
      }
      const s = await e.json();
      this._summary = s.summary;
    } catch (r) {
      this._error = r instanceof Error ? r.message : "Failed to get AI summary.";
    } finally {
      this._loading = !1;
    }
  }
  _close() {
    this.modalContext?.reject();
  }
  render() {
    const r = It(this.data?.level ?? "");
    return f`
      <uui-dialog-layout headline="AI Log Analysis">
        <div class="content-wrapper">
          <div class="log-section">
            <div class="log-meta">
          ${this.data?.level ? f`<span
                class="level-badge"
                style="background:${r}"
                >${this.data.level}</span
              >` : z}
          ${this.data?.timestamp ? f`<span>${this.data.timestamp}</span>` : z}
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
                  ${zt(Dt(this._summary))}
                    </div>
                  `}
          </div>
        </div>

        ${!this._loading && !this._error ? f`
              <uui-button
                slot="actions"
                look="secondary"
                label="Re-analyse"
                @click=${this._fetchSummary}
              >
                <uui-icon name="icon-refresh"></uui-icon>
                Re-analyse
              </uui-button>
            ` : z}
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
b.styles = at`
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
R([
  B()
], b.prototype, "_summary", 2);
R([
  B()
], b.prototype, "_loading", 2);
R([
  B()
], b.prototype, "_error", 2);
b = R([
  ct("log-ai-summary-dialog")
], b);
export {
  b as default
};
//# sourceMappingURL=log-ai-summary-dialog.element-CgLC94cs.js.map
