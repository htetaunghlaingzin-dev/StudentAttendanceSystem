using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace StudentAttendanceSystem.Helpers;

public static class ExcelExporter
{
    public static void Export(string path,string sheetName,IReadOnlyList<string> headers,IEnumerable<IReadOnlyList<object?>> rows)
    {
        using var archive=ZipFile.Open(path,ZipArchiveMode.Create);
        Write(archive,"[Content_Types].xml","<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
        Write(archive,"_rels/.rels","<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
        Write(archive,"xl/workbook.xml",$"<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"{Escape(sheetName[..Math.Min(31,sheetName.Length)])}\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
        Write(archive,"xl/_rels/workbook.xml.rels","<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
        Write(archive,"xl/styles.xml","<?xml version=\"1.0\" encoding=\"UTF-8\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font/><font><b/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"2\"><xf xfId=\"0\"/><xf xfId=\"0\" fontId=\"1\" applyFont=\"1\"/></cellXfs></styleSheet>");
        var xml=new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews><sheetData>");
        AddRow(xml,headers.Cast<object?>().ToArray(),1,true);var rowNumber=2;
        foreach(var row in rows)AddRow(xml,row,rowNumber++,false);
        xml.Append("</sheetData><autoFilter ref=\"A1:").Append(ColumnName(headers.Count)).Append(rowNumber-1).Append("\"/></worksheet>");
        Write(archive,"xl/worksheets/sheet1.xml",xml.ToString());
    }
    static void AddRow(StringBuilder xml,IReadOnlyList<object?> cells,int row,bool header){xml.Append("<row r=\"").Append(row).Append("\">");for(var i=0;i<cells.Count;i++){var reference=ColumnName(i+1)+row;var value=cells[i];if(value is byte or short or int or long or float or double or decimal)xml.Append("<c r=\"").Append(reference).Append("\"").Append(header?" s=\"1\"":"").Append("><v>").Append(Convert.ToString(value,CultureInfo.InvariantCulture)).Append("</v></c>");else xml.Append("<c r=\"").Append(reference).Append("\" t=\"inlineStr\"").Append(header?" s=\"1\"":"").Append("><is><t>").Append(Escape(Convert.ToString(value,CultureInfo.CurrentCulture)??"")).Append("</t></is></c>");}xml.Append("</row>");}
    static string ColumnName(int number){var value="";while(number>0){number--;value=(char)('A'+number%26)+value;number/=26;}return value;}
    static string Escape(string value)=>SecurityElement.Escape(value)??"";
    static void Write(ZipArchive archive,string name,string content){var entry=archive.CreateEntry(name,CompressionLevel.Optimal);using var writer=new StreamWriter(entry.Open(),new UTF8Encoding(false));writer.Write(content);}
}
