# opencode-go Models Comparison

*Zestawienie modeli dostępnych w subskrypcji OpenCode Go ($5/1. mies → $10/mies) vs ceny OpenRouter/direct API. Dane z: opencode.ai/docs/go/, openrouter.ai, mastra.ai, GitHub issues (stan na czerwiec 2026).*

---

## Tabela porównawcza

| Model | Context | **opencode-go Pricing** (per 1M tokens) | **OpenRouter/Direct Pricing** (per 1M) | Capabilities | **opencode-go Quota** (5h / week / month) | Notes |
|-------|---------|------------------------------------------|----------------------------------------|--------------|-------------------------------------------|-------|
| **GLM-5.1** | 203K | Input: $1.40 / Output: $4.40 / Cache Read: $0.26 | ~$1.00 / $4.00 (Z.ai direct) | Reasoning, math, long-horizon coding agents, tool calling | 880 / 2,150 / 4,300 | Best for repo-scale engineering; text-only |
| **GLM-5** | 203K | Input: $1.00 / Output: $3.20 / Cache Read: $0.20 | ~$1.00 / $3.00 | Reasoning, coding | 1,150 / 2,880 / 5,750 | Slightly cheaper than 5.1 |
| **Kimi K2.6** | 262K | Input: $0.95 / Output: $4.00 / Cache Read: $0.16 | ~$0.68 / $2.39 (OpenRouter) | **Multimodal** (native vision), tool-heavy agents, 256K context, MoE 1T/32B | 1,150 / 2,880 / 5,750 | Best for agentic coding with screenshots/tools |
| **Kimi K2.5** | 262K | Input: $0.60 / Output: $3.00 / Cache Read: $0.10 | ~$0.60 / $3.00 | Multimodal, long docs, code tasks | 1,850 / 4,630 / 9,250 | Cheaper than K2.6 |
| **MiMo-V2.5** | 1.0M | Input: $0.14 / Output: $0.28 / Cache Read: $0.0028 | ~$0.14 / $0.28 | **Cheapest**, 1M context, coding, MIT license | 30,100 / 75,200 / 150,400 | Best value for high-volume routine tasks |
| **MiMo-V2.5-Pro** | 1.0M | Input: $1.74 / Output: $3.48 / Cache Read: $0.0145 | ~$1.00 / $3.00 | Coding, 1M context, strong benchmarks, MIT | 3,250 / 8,150 / 16,300 | Pro version - higher quality, 10x cost of base |
| **MiniMax M2.5** | 205K | Input: $0.30 / Output: $1.20 / Cache Read: $0.06 / Cache Write: $0.375 | ~$0.30 / $1.20 | Engineering workflows, coding agents, good tool use | 6,300 / 15,900 / 31,800 | Strong for day-to-day coding agents |
| **MiniMax M2.7** | 205K | Input: $0.30 / Output: $1.20 / Cache Read: $0.06 / Cache Write: $0.375 | ~$0.30 / $1.20 | Similar to M2.5, newer variant | 3,400 / 8,500 / 17,000 | Slightly lower quota than M2.5 |
| **MiniMax M3** | 205K | Input: $0.60 / Output: $2.40 / Cache Read: $0.12 / Cache Write: $0.75 | — | Flagship MiniMax | 1,400 / 3,500 / 7,000 | Higher cost, lower quota |
| **Qwen3.7 Plus** | 262K* | Input: $0.40 / Output: $1.60 / Cache Read: $0.04 / Cache Write: $0.50 | — | **Multimodal** (text/video/image), 1M context*, fast (158 tok/s) | 4,300 / 10,800 / 21,600 | Best for frontend from screenshots; proprietary |
| **Qwen3.6 Plus** | 262K* | Input: $0.50 / Output: $3.00 / Cache Read: $0.05 / Cache Write: $0.625 | ~$0.50 / $3.00 | Multimodal, fast, best tool compat (Claude Code, Cline) | 3,300 / 8,200 / 16,300 | Slightly cheaper input than 3.7 Plus |
| **Qwen3.5 Plus** | 262K | Input: $0.20 / Output: $1.00 | — | General coding, good default | 10,200 / 25,200 / 50,500 | Highest quota in Go |
| **DeepSeek V4 Pro** | 1.0M | Input: $1.74 / Output: $3.48 / Cache Read: $0.0145 | **$0.435 / $0.87** (permanent 75% off) | Top reasoning, 1M context, native CoT, MIT | 3,450 / 8,550 / 17,150 | **⚠️ Go uses pre-discount pricing** — actual API is 4x cheaper |
| **DeepSeek V4 Flash** | 1.0M | Input: $0.14 / Output: $0.28 / Cache Read: $0.0028 | ~$0.14 / $0.28 | Best value V4, 1M context, native reasoning, MIT | 31,650 / 79,050 / 158,150 | Cheapest flagship-tier model |

> *Qwen3.6/3.7 Plus: oficjalnie 262K w Go, ale upstream deklaruje 1M kontekst.

---

## Kluczowe parametry subskrypcji Go

| Parametr | Wartość |
|----------|---------|
| **Cena** | $5 (pierwszy miesiąc) → $10/miesiąc |
| **Pula miesięczna** | $60 równowartości API |
| **Okno 5h (rolling)** | $12 |
| **Tydzień (rolling)** | $30 |
| **Limity** | Wspólne dla wszystkich modeli (dollar-equivalent) |
| **Endpointy** | OpenAI-compat (GLM, Kimi, DeepSeek, MiMo) / Anthropic-compat (MiniMax, Qwen) |
| **Regiony serwerów** | US, EU, Singapore |

---

## Największy / najmniejszy quota (requests/miesiąc)

| Kategoria | Modele |
|-----------|--------|
| **Najwięcej requestów** | DeepSeek V4 Flash (~158k), MiMo-V2.5 (~150k), Qwen3.5 Plus (~50k) |
| **Najmniej requestów** | GLM-5.1 (~4.3k), Qwen3.7 Max (~4.8k), MiniMax M3 (~7k) |

---

## ⚠️ Problem z DeepSeek V4 Pro w Go

OpenCode Go nalicza **stare ceny** ($1.74 input / $3.48 output) zamiast aktualnych permanentnych cen z 75% zniżką ($0.435 / $0.87).

**Wynik**: Użytkownik otrzymuje **~75% mniej requestów** (3,450/5h zamiast ~13,700/5h) w tym samym limicie $12.

> Szczegóły: [GitHub #29008](https://github.com/anomalyco/opencode/issues/29008) — Go zaktualizowało tylko cache hit (z $0.145 na $0.0145), ale nie przekażało 75% zniżki na input/output.

**Rekomendacja**: Do DeepSeek V4 Pro używaj bezpośredniego API / OpenRouter, nie Go.

---

## Rekomendacje wyboru modelu

| Przypadek użycia | Model do wybrania | Dlaczego |
|------------------|-------------------|----------|
| **Codzienne kodowanie, duży wolumen** | `deepseek-v4-flash` lub `mimo-v2.5` | Najtańsze, najwyższy limit, 1M kontekst |
| **Agenty z narzędziami, screenshots** | `kimi-k2.6` | Multimodal, mocne tool use, 256K kontekst |
| **Inżynieria repo-scale, długie sesje** | `glm-5.1` | Tekst-only, ale najlepsze na long-horizon engineering |
| **Frontend z designów (UI → kod)** | `qwen3.7-plus` / `qwen3.6-plus` | Multimodal, szybkie (158 tok/s), dobre tooling |
| **Top reasoning, 1M kontekst** | `deepseek-v4-pro` (bez Go!) | Bezpośrednio API = 4x tańsze, MIT license |
| **Balans jakości/ceny (default)** | `qwen3.6-plus` lub `minimax-m2.5` | Dobre wyniki, rozsądne limity |

---

## Źródła

- [opencode.ai/docs/go/](https://opencode.ai/docs/go/) — oficjalna dokumentacja Go (ceny, limity, endpointy)
- [mastra.ai/models/providers/opencode-go](https://mastra.ai/models/providers/opencode-go) — tabela cen opencode-go
- [openrouter.ai/pricing](https://openrouter.ai/pricing) — ceny OpenRouter
- [GitHub #29008](https://github.com/anomalyco/opencode/issues/29008) — problem z DeepSeek V4 Pro pricing
- [Thomas Wiegold blog](https://thomas-wiegold.com/blog/opencode-go-review/) — analiza quota i kosztów
- [whichllm.io](https://whichllm.io/models/opencode-go-minimax-m2-5) — weryfikacja kontekstu i cen

---

*Ostatnia aktualizacja: czerwiec 2026. Ceny i limity mogą się zmieniać — zawsze weryfikuj w oficjalnej dokumentacji przed decyzjami.*