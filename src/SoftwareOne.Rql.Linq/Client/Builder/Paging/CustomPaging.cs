#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public record CustomPaging(int Limit, int Offset) : Paging;