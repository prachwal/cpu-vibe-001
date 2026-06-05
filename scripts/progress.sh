#!/bin/bash
# scripts/progress.sh — Postęp implementacji CPU-VIBE-001
# Uruchamiać przed każdym commit.

set -e

INSTR_DIR="src/Mos6502/Instructions"
TEST_DIR="tests/Mos6502.Tests"

echo "============================================"
echo "  CPU-VIBE-001 — Postęp implementacji"
echo "============================================"
echo ""

# --- Instrukcje ---
TOTAL_INSTR=$(find "$INSTR_DIR" -name "*.cs" ! -name "Instruction*" | wc -l)
IMPLEMENTED=$(grep -rl "cpu\.Cycles" "$INSTR_DIR" --include="*.cs" 2>/dev/null | grep -v "InstructionTable\|InstructionHandler" | wc -l)
STUB=$((TOTAL_INSTR - IMPLEMENTED))

echo "INSTRUKCJE"
echo "  Zaimplementowane:  $IMPLEMENTED / $TOTAL_INSTR"
echo "  Stub (puste):      $STUB / $TOTAL_INSTR"
echo ""

# --- Procent ---
if [ "$TOTAL_INSTR" -gt 0 ]; then
    PCT=$((IMPLEMENTED * 100 / TOTAL_INSTR))
    echo "  POSTEP:            ${PCT}%"
else
    echo "  POSTEP:            0%"
fi
echo ""

# --- Per mnemonic ---
echo "PER MNEMONIC"
printf "  %-8s %5s %5s %5s %6s\n" "MNEMONIC" "IMPL" "ALL" "DONE" "PCT"
echo "  -------- ----- ----- ----- ------"

# Zbierz unikalne mnemonic z katalogów
for dir in "$INSTR_DIR"/*/; do
    mnem=$(basename "$dir")
    # Pomiń katalogi niezwiązane z instrukcjami
    case "$mnem" in
        Adc|And|Asl|Bcc|Bcs|Beq|Bit|Bmi|Bne|Bpl|Brk|Bvc|Bvs|Clc|Cld|Cli|Clv|Cmp|Cpx|Cpy|Dec|Dex|Dey|Eor|Inc|Inx|Iny|Jmp|Jsr|Lda|Ldx|Ldy|Lsr|Nop|Ora|Pha|Php|Pla|Plp|Rol|Ror|Rti|Rts|Sbc|Sec|Sed|Sei|Sta|Stx|Sty|Tax|Tay|Tsx|Txa|Txs|Tya)
            ;;
        *)
            continue
            ;;
    esac

    all=$(find "$dir" -name "*.cs" | wc -l)
    done=$(grep -l "cpu\.Cycles" "$dir"*.cs 2>/dev/null | wc -l)
    remaining=$((all - done))

    if [ "$all" -gt 0 ]; then
        pct=$((done * 100 / all))
    else
        pct=0
    fi

    printf "  %-8s %5d %5d %5d %5d%%\n" "$mnem" "$remaining" "$all" "$done" "$pct"
done
echo ""

# --- Testy ---
echo "TESTY"
TEST_COUNT=$(grep -r "\[Fact\]\|\[Theory\]" "$TEST_DIR" --include="*.cs" 2>/dev/null | wc -l)
echo "  Testow:            $TEST_COUNT"
echo ""

# --- Pliki ---
TOTAL_CS=$(find src/ -name "*.cs" | wc -l)
echo "PLIKI"
echo "  Plikow .cs (src):  $TOTAL_CS"
echo ""

echo "============================================"
echo "  Uruchom: bash scripts/progress.sh"
echo "============================================"
