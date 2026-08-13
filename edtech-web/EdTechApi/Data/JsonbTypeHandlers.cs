using System.Data;
using System.Text.Json;
using Dapper;
using EdTechApi.Models;

namespace EdTechApi.Data;

public static class JsonbTypeHandlers
{
    public static void Register()
    {
        SqlMapper.AddTypeHandler(new JsonbListIntHandler());
        SqlMapper.AddTypeHandler(new JsonbListAnswerHandler());
    }
}

public class JsonbListIntHandler : SqlMapper.TypeHandler<List<int>>
{
    public override List<int> Parse(object value)
    {
        return value switch
        {
            string s when string.IsNullOrWhiteSpace(s) => new(),
            string s => JsonSerializer.Deserialize<List<int>>(s) ?? new(),
            List<int> list => list,
            _ => new()
        };
    }

    public override void SetValue(IDbDataParameter parameter, List<int>? value)
    {
        parameter.Value = value ?? (object)DBNull.Value;
    }
}

public class JsonbListAnswerHandler : SqlMapper.TypeHandler<List<ExamSession.Answer>>
{
    public override List<ExamSession.Answer> Parse(object value)
    {
        return value switch
        {
            string s when string.IsNullOrWhiteSpace(s) => new(),
            string s => JsonSerializer.Deserialize<List<ExamSession.Answer>>(s) ?? new(),
            List<ExamSession.Answer> list => list,
            _ => new()
        };
    }

    public override void SetValue(IDbDataParameter parameter, List<ExamSession.Answer>? value)
    {
        parameter.Value = value ?? (object)DBNull.Value;
    }
}
