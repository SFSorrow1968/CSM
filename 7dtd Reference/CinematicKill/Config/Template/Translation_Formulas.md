# Google Sheets Translation Formulas

Import Localization_Template.txt into Google Sheets, then paste these formulas into row 2 of each language column.

## COPY-PASTE FORMULAS (paste into row 2 of each column)

### C2 (german):
=MAP($B$2:$B, LAMBDA(x, IF(x="","", IFERROR(GOOGLETRANSLATE(x,"en","de"),""))))

### D2 (french):
=MAP($B$2:$B, LAMBDA(x, IF(x="","", IFERROR(GOOGLETRANSLATE(x,"en","fr"),""))))

### E2 (italian):
=MAP($B$2:$B, LAMBDA(x, IF(x="","", IFERROR(GOOGLETRANSLATE(x,"en","it"),""))))

### F2 (spanish):
=MAP($B$2:$B, LAMBDA(x, IF(x="","", IFERROR(GOOGLETRANSLATE(x,"en","es"),""))))

### G2 (japanese):
=MAP($B$2:$B, LAMBDA(x, IF(x="","", IFERROR(GOOGLETRANSLATE(x,"en","ja"),""))))

### H2 (koreana):
=MAP($B$2:$B, LAMBDA(x, IF(x="","", IFERROR(GOOGLETRANSLATE(x,"en","ko"),""))))

### I2 (polish):
=MAP($B$2:$B, LAMBDA(x, IF(x="","", IFERROR(GOOGLETRANSLATE(x,"en","pl"),""))))

### J2 (brazilian):
=MAP($B$2:$B, LAMBDA(x, IF(x="","", IFERROR(GOOGLETRANSLATE(x,"en","pt"),""))))

### K2 (russian):
=MAP($B$2:$B, LAMBDA(x, IF(x="","", IFERROR(GOOGLETRANSLATE(x,"en","ru"),""))))

### L2 (turkish):
=MAP($B$2:$B, LAMBDA(x, IF(x="","", IFERROR(GOOGLETRANSLATE(x,"en","tr"),""))))

### M2 (schinese):
=MAP($B$2:$B, LAMBDA(x, IF(x="","", IFERROR(GOOGLETRANSLATE(x,"en","zh-CN"),""))))

### N2 (tchinese):
=MAP($B$2:$B, LAMBDA(x, IF(x="","", IFERROR(GOOGLETRANSLATE(x,"en","zh-TW"),""))))

---

## INSTRUCTIONS
1. Import Localization_Template.txt into Google Sheets (File > Import)
2. Copy each formula above and paste into the corresponding cell (C2, D2, etc.)
3. Wait for translations to populate (may take a moment)
4. Select all translated cells, Copy, then Paste Values Only (Ctrl+Shift+V) to freeze them
5. Export as CSV
6. Rename to Localization.txt and replace the one in Config folder
