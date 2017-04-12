rem SphinxRulesGenerator.exe rules Mapping.txt DNBrules-as-of-15Q3.xlsx "FINREP" SphinxRulesOutput.FINREP.xsr > GenerationResults.FINREP.txt
rem SphinxRulesGenerator.exe rules Mapping.txt DNBrules-as-of-15Q3.xlsx "COREP" SphinxRulesOutput.COREP.xsr > GenerationResults.COREP.txt
rem SphinxRulesGenerator.exe rules Mapping.txt "Additional validation rules effective from Q3 2015 published by DNB on 20151106.xlsx" "COREPFINREP IFRS" SphinxRulesOutput.COREPFINREPIFRS.xsr > GenerationResults.COREPFINREPIFRS.txt
rem SphinxRulesGenerator.exe rules Mapping.txt "Checks_bestand_voor_e-line_final_(06-11-2015)_tcm46-333427.xlsx" "COREP" SphinxRulesOutput.COREP.xsr > GenerationResults.COREP.txt
rem SphinxRulesGenerator.exe rules Mapping.txt "Additional_DNB_checks_2015Q4_(29-12-2015)_tcm46-335772.xlsx" "COREP" SphinxRulesOutput_Additional.COREP.xsr > GenerationResults_Additional.COREP.txt

REM 2016 Q1

REM Consistency checks
SphinxRulesGenerator.exe rules Mapping.txt "Validatiecontroles_2016_Q2_updated_by_Venkat.xlsx" "DNB_Consistency_check" SphinxRulesOutput_Consistency.xsr "1.2 DNB Consistency checks" > GenerationResults_Consistency.txt

REM Triangle checks
SphinxRulesGenerator.exe rules Mapping.txt "Validatiecontroles_2016_Q2_updated_by_Venkat.xlsx" "DNB_Triangle_check" SphinxRulesOutput_Triangle.xsr "1.3 DNB Triangle checks" > GenerationResults_Triangle.txt

