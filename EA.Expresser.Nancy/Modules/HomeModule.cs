namespace EA.Expresser.NancyExample {
    using EA.Expresser.Libs;
    using Nancy;
    using Simple.Data;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    public class HomeModule : NancyModule {
        public HomeModule() {
            Get["/"] = _ => View["Index"];

            Get["/db/{schema}/{table}"] = _ => {
                var db = Database.Open();
                var table = db[_.schema.ToString()][_.table.ToString()];
                var expr = Request.Query.expression.ToString();
                List<dynamic> res = table.All()
                                         .Where(ExParser.Parse(query: expr, table: table))
                                         .ToList();
                return Response.AsJson(res);
            };
        }
    }
}