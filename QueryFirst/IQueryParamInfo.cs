﻿namespace QueryFirst
{
    public interface IQueryParamInfo
    {
        string CSType { get; set; }
        //bool ExplicitlyDeclared { get; set; }
        int Length { get; set; }
        int Precision { get; set; }
        int Scale { get; set; }
        string CSName { get; set; }
        string DbName { get; set; }
        string DbType { get; set; }
		bool AllowDbNull { get; set; }
        //string SqlTypeAndLength { get; set}
    }
}