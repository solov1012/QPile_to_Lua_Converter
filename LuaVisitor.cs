using System;
using System.Collections.Generic;
using System.Text;
using Antlr4.Runtime.Misc;

namespace Converter
{
    class LuaVisitor : QPILE_converterV2BaseVisitor<string>
    {
        string before_main = "";
        string main_funk = "";
        string methods = "";
        string create_table = "";
        string create_table_delta = "";
        string before_equal = "";
        string end_script = "";
        string table = "";
        int i = 0;
        bool twice_visit = false;
        List<string> mass = new List<string>();

        public override string VisitProgram([NotNull] QPILE_converterV2Parser.ProgramContext context)
        {
            string s = "";
            foreach (var child in context.children) { var str = Visit(child); s += str + "\n"; }
            main_funk = "function main()\n" + main_funk + "\nend;";
            create_table += create_table_delta;
            create_table = "function CreateTable()" + "\n" + create_table;
            create_table = create_table + "\nend;";
            return before_main + main_funk + "\n" + methods + "\n" + create_table + "\n" + end_script;

        }

        public override string VisitDeclarationBlock([NotNull] QPILE_converterV2Parser.DeclarationBlockContext context)
        {
            //res = context.children[1].ToString() + " " + context.children[2].ToString() + " " + context.children[3].ToString();
            table = context.children[1].GetText();
            int n = table.LastIndexOf(";");
            if (n == table.Length - 1)
            {
                table = table.Remove(n);
            }
            create_table += "    " + table + " = AllocTable();\n";
            main_funk += "    CreateTable();\n";
            string window_caption = context.children[5].GetText();
            window_caption = window_caption.Remove(window_caption.Length - 1);
            create_table_delta += "    t = CreateWindow(" + table + ");\n" + "    SetWindowCaption(" + table + "," + '"'+window_caption+'"' + ");\n";
            end_script += "function OnStop()\n" + "    sIsRun = false;\n" + "    if " + table + " ~= nil then\n";
            end_script += "        DestroyTable(" + table + ");\n" + "    end;\n" + "end;";
            return table + " = {}";
            return base.VisitDeclarationBlock(context);
        }

        public override string VisitParamsList([NotNull] QPILE_converterV2Parser.ParamsListContext context)
        {
            string res = "";
            foreach (var elem in context.children)
            {
                if (elem.GetText() != "\n")
                {
                    var st = Visit(elem);
                    res += st + "\n";
                }
            }
            int count = 0;
            foreach (var u in mass)
            {
                count++;
                before_main += " " + '"' + u + '"';
                if (count != mass.Count)
                {
                    before_main += ",";
                }
                if ((count % 3 == 0) && (count != mass.Count))
                {
                    before_main += "\n";
                }
            }
            before_main = "ParameterNames={\n" + before_main + "\n}\n";
            return res;
        }

        public override string VisitParamsBlock([NotNull] QPILE_converterV2Parser.ParamsBlockContext context)
        {
            string m = context.children[1].GetText();
            if (mass.Contains(m) == false)
            {
                mass.Add(m);
            }
            i++;
            string st = "";
            st += "addcolumn(" + context.children[1].GetText() + ",)";
            string name_column = context.children[5].GetText();
            int n = name_column.LastIndexOf(";");
            if (n == name_column.Length - 1)
            {
                name_column = name_column.Remove(n);
            }
            string type_column_qpile = context.children[11].GetText();
            string type_column_lua = type_column_qpile;
            string size_string = "";
            if ((type_column_qpile.Contains("STRING(")) && (type_column_qpile.Contains(")")))
            {

                size_string = type_column_qpile.Remove(type_column_qpile.Length - 1);
                int number_left_parenthesis = size_string.IndexOf("(");
                size_string = size_string.Remove(0, number_left_parenthesis + 1);

                //size_string = size_string.Remove(size_string.Length);
                //size_string = context.children[13].GetText();
                type_column_lua = "QTABLE_STRING_TYPE";
            }
            if ((type_column_qpile.Contains("NUMERIC(")) && (type_column_qpile.Contains(")")))
            {

                int comma_number = type_column_qpile.IndexOf(",");
                size_string = type_column_qpile.Remove(comma_number);
                int number_left_parenthesis = size_string.IndexOf("(");
                size_string = size_string.Remove(0, number_left_parenthesis + 1);

                //size_string = context.children[13].GetText();
                type_column_lua = "QTABLE_DOUBLE_TYPE";
            }
            create_table += "    AddColumn(" + table + ", " + i.ToString() + ", " + '"' + name_column + '"' + ", true, " + type_column_lua + ", " + size_string + ");\n";
            //create_table += "    AddColumn(" + table + ", " + i.ToString() + ", " + name_column + ", true, " + type_column_lua + ", " + size_string + ");\n";
            return st;
            return base.VisitParamsBlock(context);
        }

        public override string VisitProgramBlock([NotNull] QPILE_converterV2Parser.ProgramBlockContext context)
        {
            var c = context.children[2];
            var vis = Visit(c);
            main_funk += vis;
            var stList = Visit(context.children[2]);
            return "function main()" + "\n" + stList + "end";
        }

        public override string VisitStatementList([NotNull] QPILE_converterV2Parser.StatementListContext context)
        {
            string res = "";
            string gt = context.GetText();
            foreach (var elem in context.children)
            {
                if (elem.GetText() != "\r\n")
                {
                    var st = Visit(elem);
                    res += st + "\n";
                }
            }
            twice_visit = true;
            /*
            if (twice_visit1[twice_visit1.Count])
            {
                twice_visit2.Add(true);
            }
            */
            return res;
        }

        public override string VisitStatement([NotNull] QPILE_converterV2Parser.StatementContext context)
        {
            if ((context.ifOperator() != null) || (context.forOperator() != null) || (context.funcDescr() != null) || (context.procedureCall() != null))
            {
                var res = Visit(context.children[0]);
                return res;
            }

            if (context.RETURN() != null)
            {
                if (twice_visit == false)
                {
                    main_funk += "    return RESULT\n";
                }
                return AddSpaces("return RESULT");
            }

            if (context.BREAK() != null)
            {
                if (twice_visit == false)
                {
                    main_funk += "    break\n";
                }
                return AddSpaces("break");
            }

            if (context.CONTINUE() != null)
            {
                if (twice_visit == false)
                {
                    main_funk += "    continue\n";
                }
                return AddSpaces("continue");
            }

            if (context.EQUAL() != null)
            {
                string str = context.GetText();
                before_equal = str.Substring(0, str.IndexOf("="));
                var exp = Visit(context.children[2]);
                /*
                if (twice_visit == false)
                {
                    main_funk += "    " + context.name().GetText() + " = " + exp + "\n";
                }
                */
                return "    " + context.name().GetText() + " = " + exp;
            }

            return base.VisitStatement(context);
        }

        public override string VisitProcedureCall([NotNull] QPILE_converterV2Parser.ProcedureCallContext context)
        {
            if (context.qProcedureCall() != null)
            {
                return Visit(context.children[0]);
            }

            var fName = Visit(context.children[0]);
            var argList1 = Visit(context.children[2]);
            return AddSpaces(fName + " (" + argList1 + ")");
        }

        public override string VisitQProcedureCall([NotNull] QPILE_converterV2Parser.QProcedureCallContext context)
        {
            if (context.MESSAGE() != null)
            {
                var argList1 = Visit(context.children[2]);
                return AddSpaces("message" + " (" + argList1 + ")");
            }

            if (context.SET_ROW_COLOR_EX() != null)
            {
                var row_number = Visit(context.children[2]);
                var background_color = context.children[4].GetText();
                var selected_background_color = context.children[6].GetText();
                var font_color = context.children[8].GetText();
                var selected_font_color = context.children[10].GetText();
                var b_color = background_color;
                var f_color = font_color;
                var sel_b_color = selected_background_color;
                var sel_f_color = selected_font_color;
                    b_color = b_color.Remove(b_color.Length - 1);
                    b_color = b_color.Substring(1);
                    f_color = f_color.Remove(f_color.Length - 1);
                    f_color = f_color.Substring(1);
                    sel_b_color = sel_b_color.Remove(sel_b_color.Length - 1);
                    sel_b_color = sel_b_color.Substring(1);
                    sel_f_color = sel_f_color.Remove(sel_f_color.Length - 1);
                    sel_f_color = sel_f_color.Substring(1);
                    return AddSpaces("SetColor" + " (" + table + ", " + row_number + ", " + b_color + ", " + f_color + ", " + sel_b_color + ", " + sel_f_color + ")");
                return AddSpaces("`" + context.GetText());
            }

            if (context.DELETE_ITEM() != null)
            {
                var index = context.children[2].GetText();
                return AddSpaces("DeleteRow(" + table + ", " + index + ")");
            }

            if (context.DELETE_ALL_ITEMS() != null)
            {
                return AddSpaces("isCleared = Clear (0)");
            }

            if (context.ADD_ITEM() != null)
            {
                var index = Visit(context.children[2]);
                var table_string = context.children[4].GetText();
                    string res = "InsertRow (" + table + ",INDEX)\n    a=0\n    for key,value in pairs(table_string)\n";
                    res += "    do\n        " + "for i=1,#ParameterNames do\n           if(ParameterNames[i] == key) then a=i\n           end\n        end\n" +
                    "        if(type(value)==" + '"' + "string" + '"' + ") then\n            SetCell(" + table + ",INDEX,a,tostring(value))\n";
                    res += "        else\n            SetCell(" + table + ",INDEX,a,tostring(value),value)\n        end\n";
                    res += "    end";
                    if (methods.Contains("AddItem(INDEX, table_string)\n    " + res + "\nend") == false)
                    {
                        methods += "function AddItem(INDEX, table_string)\n    " + res + "\nend\n";
                    }
                    return AddSpaces("AddItem(" + index.ToString() + ", " + table_string + ")");
                return AddSpaces("`" + context.GetText());
            }

            if (context.MODIFY_ITEM() != null)
            {
                var index = Visit(context.children[2]);
                var table_string = context.children[4].GetText();
                    string res = "i=1\n    for key,value in pairs(table_string)\n";
                    res += "    do\n        if(type(value)==" + '"' + "string" + '"' + ") then\n            SetCell(" + table + ",INDEX,i,tostring(value))\n";
                    res += "        else\n            SetCell(" + table + ",INDEX,i,tostring(value),value)\n        end\n";
                    res += "        i=i+1\n    end";
                    if (methods.Contains("ModifyItem(INDEX, table_string)\n    " + res + "\nend") == false)
                    {
                        methods += "function ModifyItem(INDEX, table_string)\n    " + res + "\nend\n";
                    }
                    return AddSpaces("Modify(" + index.ToString() + ", " + table_string + ")");
                return AddSpaces("`" + context.GetText());
            }

            return base.VisitQProcedureCall(context);
        }

        public override string VisitFuncDescr([NotNull] QPILE_converterV2Parser.FuncDescrContext context)
        {
            var name = Visit(context.children[1]);
            var fargList = Visit(context.children[3]);
            var statementList = Visit(context.children[6]);
            string res = "#@" + "function " + name + " (" + fargList + ") " + "\n" + statementList + "end" + "@#";
            return res;
        }

        public override string VisitIfOperator([NotNull] QPILE_converterV2Parser.IfOperatorContext context)
        {
            var condition = Visit(context.children[1]);
            condition = condition.Replace("=", "==");
            var ifStatement = Visit(context.children[3]);
            var elseStatement = Visit(context.children[7]);
            var res = "if " + condition + " then" + "\n" +
                  ifStatement +
                  "else" + "\n" +
                   elseStatement + "end";
            return AddSpaces(res);
        }

        public override string VisitForOperator([NotNull] QPILE_converterV2Parser.ForOperatorContext context)
        {
            string s1 = context.GetText();
            if (context.FOR() != null)
            {
                var name = Visit(context.children[1]);
                var expression1 = Visit(context.children[3]);
                var expression2 = Visit(context.children[5]);
                var statement = Visit(context.children[7]);
                var res = "for " + name + " = " + expression1 + "," + expression2 + " do" + "\n" +
                     statement +
                     "end";
                return AddSpaces(res);
            }

            return null; //foreach не обрабатывается
        }

        public override string VisitCondition([NotNull] QPILE_converterV2Parser.ConditionContext context)
        {
            if (context.primaryCondition() != null)
            {
                return Visit(context.children[0]);
            }

            if (context.OR() != null)
            {
                var condition1 = Visit(context.children[0]);
                var condition2 = Visit(context.children[2]);
                return condition1 + " or " + condition2;
            }

            if (context.AND() != null)
            {
                var condition1 = Visit(context.children[0]);
                var condition2 = Visit(context.children[2]);
                return condition1 + " and " + condition2;
            }

            if (context.LPAREN() != null)
            {
                var condition = Visit(context.children[1]);
                return " (" + condition + ") ";
            }

            return Visit(context.children[0]);
        }

        public override string VisitPrimaryCondition([NotNull] QPILE_converterV2Parser.PrimaryConditionContext context)
        {
            var expression1 = Visit(context.children[0]);
            var expression2 = Visit(context.children[2]);
            string relation = null;
            if (context.EQUAL() != null)
            {
                relation = " = ";
            }

            if (context.LE() != null)
            {
                relation = " <= ";
            }

            if (context.LT() != null)
            {
                relation = " < ";
            }

            if (context.GE() != null)
            {
                relation = " >= ";
            }

            if (context.GT() != null)
            {
                relation = " > ";
            }

            if (context.NOT_EQUAL() != null)
            {
                relation = " ~= ";
            }

            return expression1 + relation + expression2;
        }

        public override string VisitExpression([NotNull] QPILE_converterV2Parser.ExpressionContext context)
        {
            if (context.PLUS() != null)
            {
                var exp = Visit(context.children[0]);
                var term = Visit(context.children[2]);
                return exp + " + " + term;
            }

            if (context.MINUS() != null)
            {
                var exp = Visit(context.children[0]);
                var term = Visit(context.children[2]);
                return exp + " - " + term;
            }

            if (context.COMPOUND() != null)
            {
                var exp = Visit(context.children[0]);
                var term = Visit(context.children[2]);
                return exp + " .. " + term;

            }
            var onlyTerm = Visit(context.children[0]);
            return onlyTerm;
        }

        public override string VisitTerm([NotNull] QPILE_converterV2Parser.TermContext context)
        {
            if (context.SLASH() != null)
            {
                var term = Visit(context.children[0]);
                var primary = Visit(context.children[2]);
                return term + " / " + primary;
            }

            if (context.STAR() != null)
            {
                var term = Visit(context.children[0]);
                var primary = Visit(context.children[2]);
                return term + " * " + primary;
            }

            var onlyPrimary = Visit(context.children[0]);
            return onlyPrimary;
        }

        public override string VisitPrimary([NotNull] QPILE_converterV2Parser.PrimaryContext context)
        {
            if (context.number() != null)
            {
                var number = Visit(context.children[0]);
                return number;
            }

            if (context.name() != null)
            {
                var name = Visit(context.children[0]);
                return name;
            }

            if (context.STRING_SYMBOLS() != null)
            {
                return context.STRING_SYMBOLS().GetText();
            }

            if (context.MINUS() != null)
            {
                var primary = Visit(context.children[1]);
                return "-" + primary;
            }

            if (context.LPAREN() != null)
            {
                var exp = Visit(context.children[1]);
                return "(" + exp + ")";
            }

            return Visit(context.children[0]);
        }

        public override string VisitFunctionCall([NotNull] QPILE_converterV2Parser.FunctionCallContext context)
        {
            if (context.qFunctionCall() == null)
            {
                var fName = Visit(context.children[0]);
                var argList1 = Visit(context.children[2]);
                return fName + " (" + argList1 + ")";
            }
            return Visit(context.children[0]);
        }

        public override string VisitQFunctionCall([NotNull] QPILE_converterV2Parser.QFunctionCallContext context)
        {
            if (context.CREATE_MAP() != null)
            {
                return "{}";
            }

            if (context.SET_VALUE() != null)
            {
                var name = Visit(context.children[2]);
                var key = context.children[4].GetText();
                var value = Visit(context.children[6]);
                return "setValue(" + name + ", " + key + ", " + value + ")" + "\n"
                    + GenerateSet_ValueFunction();
            }

            if (context.GET_INFO_PARAM() != null)
            {
                var str = context.children[2].GetText();
                return "getInfoParam" + " (" + str + ")";
            }

            if (context.GET_COLLECTION_COUNT() != null)
            {
                var str = context.children[2].GetText();
                return "#" + str + "+1";
            }

            if (context.GET_VALUE() != null)
            {
                var str1 = context.children[2].GetText();
                var str2 = context.children[4].GetText();
                return str1 + "[" + str2 + "]";
            }

            if (context.GET_ITEM() != null)
            {
                var str1 = context.children[2].GetText();
                var str2 = context.children[4].GetText();
                return "getItem(" + str1 + "," + str2 + ")";
            }

            if (context.GET_NUMBER_OF() != null)
            {
                var str1 = context.children[2].GetText();
                return "getNumberOf(" + str1 + ")";
            }

            if (context.REMOVE_COLLECTION_ITEM() != null)
            {
                var str1 = context.children[2].GetText();
                var str2 = context.children[4].GetText();
                    string res = "";
                    res += "collectionName1 = {}\n    a=0\n    for j=0, #collectionName do\n        if j == index then\n            a=1\n";
                    res += "        else\n            collectionName1[j-a]=collectionName[j]\n    end\n";
                    if (methods.Contains(res) == false)
                    {
                        methods += "function RemoveCollectionItem(collectionName,index)\n    " + res + "end;\n";

                    }
                    return "RemoveCollectionItem(" + str1 + "," + str2 + ")";
            }

            if (context.INSERT_COLLECTION_ITEM() != null)
            {
                var str1 = context.children[2].GetText();
                var str2 = context.children[4].GetText();
                var str3 = context.children[6].GetText();
                    string res = "";
                    res += "collectionName1 = {}\n    a=0\n    for j=0, #collectionName do\n        if j == index then\n            a=1;\n";
                    res += "            collectionName1[j]=value\n        end\n        collectionName1[j+a]=collectionName[j]\n    end\n    return collectionName1\n";
                    if (methods.Contains(res) == false)
                    {
                        methods += "function InsertCollectionItem(collectionName,index,value)\n    " + res + "end;\n";

                    }
                    return "InsertCollectionItem(" + str1 + "," + str2 + "," + str3 + ")";
            }

            if (context.GET_COLLECTION_ITEM() != null)
            {
                var str1 = context.children[2].GetText();
                var str2 = context.children[4].GetText();
                    return str1 + "[" + str2 + "]";
            }

            if (context.SET_COLLECTION_ITEM() != null)
            {
                var str1 = context.children[2].GetText();
                var str2 = context.children[4].GetText();
                var str3 = context.children[6].GetText();
                    string res = "";
                    res += "collectionName1 = {}\n    for j = 0,#collectionName do\n        if j == index then\n            collectionName1[j]=value\n";
                    res += "        else\n            collectionName1[j]=collectionName[j]\n        end\n    end\n    return collectionName1\n";
                    if (methods.Contains(res) == false)
                    {
                        methods += "function SetCollectionItem(collectionName,index,value)\n    " + res + "end;\n";

                    }
                    return "SetCollectionItem(" + str1 + "," + str2 + "," + str3 + ")";
            }

            if (context.CREATE_COLLECTION() != null)
            {
                return "{}";
            }

            if (context.SUBSTR() != null)
            {
                string str;
                if (context.STRING_SYMBOLS() != null)
                {
                    str = context.children[2].GetText();
                }
                else
                {
                    str = Visit(context.children[2]);
                }
                var index = Visit(context.children[4]);
                var length = Visit(context.children[6]);
                return "string.sub" + "(" + str + ", " + index + ", " + index + " + " + length + ")";

            }

            return base.VisitQFunctionCall(context);
        }

        public override string VisitArgList1([NotNull] QPILE_converterV2Parser.ArgList1Context context)
        {
            if (context.argList1() == null)
            {
                return Visit(context.children[0]);
            }

            var arglist1 = Visit(context.children[0]);
            var expression = Visit(context.children[2]);
            return arglist1 + ", " + expression;
        }

        public override string VisitFargList([NotNull] QPILE_converterV2Parser.FargListContext context)
        {
            if (context.fargList() == null)
            {
                return Visit(context.children[0]);
            }

            var fargList = Visit(context.children[0]);
            var name = Visit(context.children[2]);
            return fargList + ", " + name;
        }

        public override string VisitFName([NotNull] QPILE_converterV2Parser.FNameContext context)
        {
            return Visit(context.children[0]);
        }

        public override string VisitName([NotNull] QPILE_converterV2Parser.NameContext context)
        {
            return context.IDENT().ToString();
        }

        public override string VisitNumber([NotNull] QPILE_converterV2Parser.NumberContext context)
        {
            if (context.NUM_INT() != null)
            {
                return context.NUM_INT().GetText();
            }

            return context.NUM_REAL().GetText();

        }

        public static string AddSpaces(string source)
        {
            string st = source + " ";
            string spaces = "    ";
            string substring = "\n";
            var indices = new List<int>();
            indices.Add(-1);
            int index = source.IndexOf(substring, 0);
            int i = 0;
            while (index > -1)
            {

                indices.Add(index);
                index = source.IndexOf(substring, index + substring.Length);
            }

            foreach (int elem in indices)
            {
                i++;
                st = st.Insert(elem + 4 * i - 3, spaces);
            }
            return st;
        }

        public static string GenerateSet_ValueFunction()
        {
            return "#@function setValue(name, key, value)" + "\n" +
                AddSpaces("name" + " [" + "key" + "]" + " = " + "value") + "\n"
                + AddSpaces("return name") + "\n" + "end@#";
        }
    }
}
