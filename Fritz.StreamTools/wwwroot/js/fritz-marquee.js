(function () {
	'use strict';
	let tmpl = document.createElement('template');
	tmpl.innerHTML = `
    <style>
      .marquee {
        margin: 0 auto;
        white-space: nowrap;
        overflow: hidden;
        padding: 10px 0px;
        background-color: var(--background-color, grey);
        color: var(--text-color, white);
        width: var(--width, 100%);
        font-family: var(--font-family, Arial);
        font-size: var(--font-size, 40pt);
      }

      .marquee span {
        display: inline-block;
        padding-left: 100%;
        animation: marquee var(--speed, 40s) linear infinite;
      }

      @keyframes marquee {
        0% {
          transform: translate(0, 0);
        }
        100% {
          transform: translate(-100%, 0);
        }
      }
    </style>

    <p class="marquee">
      <span>
        <slot>Did you forget to edit me?</slot>
      </span>
    </p>
  `;
	class fritzMarquee extends HTMLElement {
		constructor() {
			super();
			const shadowRoot = this.attachShadow({ mode: 'open' });
			shadowRoot.appendChild(tmpl.content.cloneNode(true));
		}
	}
	customElements.define('fritz-marquee', fritzMarquee);
}());
