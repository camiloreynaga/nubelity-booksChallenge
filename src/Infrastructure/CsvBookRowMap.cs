using Application.DTOs.Books;
using CsvHelper.Configuration;

namespace Infrastructure;

public class CsvBookRowMap : ClassMap<CsvBookRowDto>
{
    public CsvBookRowMap()
    {
        Map(m => m.Isbn).Name("ISBN").Index(0);
        Map(m => m.Title).Name("Title").Index(1);
        Map(m => m.PublicationYear).Name("PublicationYear").Index(2);
        Map(m => m.PageNumber).Name("PageNumber").Index(3).Optional();
        Map(m => m.AuthorName).Name("AuthorName").Index(4);
    }
}

