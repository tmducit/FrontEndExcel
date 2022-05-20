using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Newtonsoft.Json;
using System.Data;

namespace FrontEndExcel.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult getInfo()
        {
            var excelPath = System.Configuration.ConfigurationManager.AppSettings["path"];
            var exists = System.IO.File.Exists(excelPath);
            System.Data.DataTable status = new System.Data.DataTable();
            System.Data.DataTable history = new System.Data.DataTable();
            if (exists)
            {
                using (Stream templateDocumentStream = System.IO.File.Open(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var list = ExcelToDataTable(templateDocumentStream, true);
                    status = list[0];
                    history = list[1];

                }
            }

            return Json(new { status = JsonConvert.SerializeObject(status), history = JsonConvert.SerializeObject(history) }, JsonRequestBehavior.AllowGet);
        }

        private List<DataTable> ExcelToDataTable(Stream stream, bool hasHeaderRow)
        {
            DataTable dtStatus = new DataTable();
            DataTable dtHis = new DataTable();
            string errorMessages = "";

            //create a new Excel package in a memorystream

            using (ExcelPackage excelPackage = new ExcelPackage(stream))
            {
                ExcelWorksheet worksheetStatus = excelPackage.Workbook.Worksheets["Status"];
                ExcelWorksheet worksheetHis = excelPackage.Workbook.Worksheets["Historys"];

                dtStatus = dataTable(dtStatus, worksheetStatus, hasHeaderRow);
                dtHis = dataTable(dtHis, worksheetHis, hasHeaderRow);

            }

            return new List<DataTable>() { dtStatus, dtHis };
        }

        private DataTable dataTable(DataTable dt, ExcelWorksheet worksheet, bool hasHeaderRow)
        {
            if (worksheet.Dimension == null) return dt;
            for (int j = worksheet.Dimension.Start.Column; j <= worksheet.Dimension.End.Column; j++)
            {
                string columnName = "Column " + j;
                var excelCell = worksheet.Cells[1, j].Value;

                if (excelCell != null)
                {
                    var excelCellDataType = excelCell;

                    //if there is a headerrow, set the next cell for the datatype and set the column name
                    if (hasHeaderRow == true)
                    {
                        excelCellDataType = worksheet.Cells[2, j].Value;

                        columnName = excelCell.ToString().Replace(" ","_");

                        //check if the column name already exists in the datatable, if so make a unique name
                        if (dt.Columns.Contains(columnName) == true)
                        {
                            columnName = columnName.Replace(" ", "_") + "_" + j;
                        }
                    }

                    //try to determine the datatype for the column (by looking at the next column if there is a header row)
                    if (excelCellDataType is DateTime)
                    {
                        dt.Columns.Add(columnName, typeof(DateTime));
                    }
                    else if (excelCellDataType is Boolean)
                    {
                        dt.Columns.Add(columnName, typeof(Boolean));
                    }
                    else if (excelCellDataType is Double)
                    {
                        //determine if the value is a decimal or int by looking for a decimal separator
                        //not the cleanest of solutions but it works since excel always gives a double
                        if (excelCellDataType.ToString().Contains(".") || excelCellDataType.ToString().Contains(","))
                        {
                            dt.Columns.Add(columnName, typeof(Decimal));
                        }
                        else
                        {
                            dt.Columns.Add(columnName, typeof(Int64));
                        }
                    }
                    else
                    {
                        dt.Columns.Add(columnName, typeof(String));
                    }
                }
                else
                {
                    dt.Columns.Add(columnName, typeof(String));
                }
            }

            //start adding data the datatable here by looping all rows and columns
            for (int i = worksheet.Dimension.Start.Row + Convert.ToInt32(hasHeaderRow); i <= worksheet.Dimension.End.Row; i++)
            {
                //create a new datatable row
                DataRow row = dt.NewRow();

                //loop all columns
                for (int j = worksheet.Dimension.Start.Column; j <= worksheet.Dimension.End.Column; j++)
                {
                    var excelCell = worksheet.Cells[i, j].Value;

                    //add cell value to the datatable
                    if (excelCell != null)
                    {
                        try
                        {
                            row[j - 1] = excelCell;
                        }
                        catch
                        {

                        }
                    }
                }

                //add the new row to the datatable
                dt.Rows.Add(row);
            }
            return dt;
        }
    }
}