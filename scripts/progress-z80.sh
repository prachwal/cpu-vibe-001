#!/bin/bash
echo "============================================"
echo "  Z80 — Postęp implementacji"
echo "============================================"
echo ""

SRC_DIR="src/Z80/Instructions"
TEST_DIR="tests/Z80.Tests"

TOTAL_OPCODES=806
IMPLEMENTED=0

echo "INSTRUKCJE"
for dir in $SRC_DIR/*/; do
    mnemonic=$(basename $dir)
    count=$(ls "$dir"/*.cs 2>/dev/null | wc -l)
    if [ $count -gt 0 ]; then
        IMPLEMENTED=$((IMPLEMENTED + count))
    fi
done

echo "  Zaimplementowane:  $IMPLEMENTED / $TOTAL_OPCODES"
echo ""

TEST_COUNT=$(grep -r "\[Fact\]\|\[Theory\]" $TEST_DIR --include="*.cs" 2>/dev/null | wc -l)
echo "TESTY"
echo "  Testów:            $TEST_COUNT"
echo ""

FILES=$(find src/Z80 -name "*.cs" -not -path "*/bin/*" -not -path "*/obj/*" 2>/dev/null | wc -l)
echo "PLIKI"
echo "  Plików .cs (src):  $FILES"
echo ""
