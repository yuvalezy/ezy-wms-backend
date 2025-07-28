using System.Data;
using System.Text;
using System.Text.Json;
using System.Xml;
using Core.Enums;
using Core.Interfaces;
using Core.Models.Settings;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Service for executing external commands
/// </summary>
public class ExternalCommandService(
    ISettings settings,
    SystemDbContext dbContext,
    IFileDeliveryService fileDeliveryService,
    IConfiguration configuration,
    ILogger<ExternalCommandService> logger) : IExternalCommandService
{
    private readonly SemaphoreSlim _semaphore = new(settings.ExternalCommands.GlobalSettings.MaxConcurrentExecutions);

    public async Task ExecuteCommandsAsync(CommandTriggerType triggerType, ObjectType objectType, Guid objectId, Dictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var commands = settings.ExternalCommands.Commands
            .Where(c => c.Enabled && c.TriggerType == triggerType && c.ObjectType == objectType)
            .ToArray();

            if (commands.Length == 0)
            {
                logger.LogDebug("No commands found for trigger {TriggerType} and object type {ObjectType}", triggerType, objectType);
                return;
            }

            logger.LogInformation("Executing {CommandCount} commands for trigger {TriggerType} and object {ObjectId}", commands.Length, triggerType, objectId);

            var tasks = commands.Select(command => ExecuteCommandInternalAsync(command, objectId, parameters, cancellationToken));
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute external commands for trigger {TriggerType} and object {ObjectId}", triggerType, objectId);
        }
    }

    public async Task ExecuteCommandAsync(string commandId, Guid objectId, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        var command = settings.ExternalCommands.Commands.FirstOrDefault(c => c.Id == commandId);
        if (command == null)
        {
            throw new ArgumentException($"Command with ID '{commandId}' not found");
        }

        if (!command.Enabled)
        {
            logger.LogWarning("Command {CommandId} is disabled", commandId);
            return;
        }

        await ExecuteCommandInternalAsync(command, objectId, parameters, cancellationToken);
    }

    public async Task ExecuteBatchCommandAsync(string commandId, Guid[] objectIds, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        var command = settings.ExternalCommands.Commands.FirstOrDefault(c => c.Id == commandId);
        if (command == null)
        {
            throw new ArgumentException($"Command with ID '{commandId}' not found");
        }

        if (!command.Enabled)
        {
            logger.LogWarning("Command {CommandId} is disabled", commandId);
            return;
        }

        if (!command.AllowBatchExecution)
        {
            throw new InvalidOperationException($"Command {commandId} does not support batch execution");
        }

        logger.LogInformation("Executing batch command {CommandId} for {ObjectCount} objects", commandId, objectIds.Length);

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var batchParameters = parameters ?? new Dictionary<string, object>();
            batchParameters["PackageIds"] = string.Join(",", objectIds.Select(id => $"'{id}'"));
            batchParameters["BatchIndex"] = DateTime.UtcNow.Ticks;

            var data = await ExecuteQueriesAsync(command, objectIds.First(), batchParameters, cancellationToken);
            var content = GenerateFileContent(command, data);
            var fileName = GenerateFileName(command, data, batchParameters);
            var filePath = await SaveContentToFileAsync(content, fileName, cancellationToken);

            try
            {
                await fileDeliveryService.DeliverFileAsync(filePath, fileName, command.Destination, cancellationToken);
                logger.LogInformation("Successfully executed batch command {CommandId}", commandId);
            }
            finally
            {
                CleanupTempFile(filePath);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IEnumerable<ExternalCommand>> GetManualCommandsAsync(ObjectType objectType, string screenName)
    {
        await Task.CompletedTask;

        return settings.ExternalCommands.Commands
        .Where(c => c.Enabled &&
                    c.TriggerType == CommandTriggerType.Manual &&
                    c.ObjectType == objectType &&
                    (c.UIConfiguration?.AllowedScreens.Contains(screenName) ?? false))
        .ToArray();
    }

    public async Task<IEnumerable<string>> ValidateCommandAsync(ExternalCommand command)
    {
        var errors = new List<string>();

        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(command.Id))
        {
            errors.Add("Command ID is required");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Command name is required");
        }

        if (string.IsNullOrWhiteSpace(command.FileNamePattern))
        {
            errors.Add("File name pattern is required");
        }

        if (command.Queries.Length == 0)
        {
            errors.Add("At least one query is required");
        }

        foreach (var query in command.Queries)
        {
            if (string.IsNullOrWhiteSpace(query.Name))
            {
                errors.Add($"Query name is required");
            }

            if (string.IsNullOrWhiteSpace(query.Query))
            {
                errors.Add($"Query SQL is required for query '{query.Name}'");
            }
        }

        if (command.Destination.Type == CommandDestinationType.FTP || command.Destination.Type == CommandDestinationType.SFTP)
        {
            if (string.IsNullOrWhiteSpace(command.Destination.Host))
            {
                errors.Add("Host is required for FTP/SFTP destinations");
            }
        }

        return errors;
    }

    private async Task ExecuteCommandInternalAsync(ExternalCommand command, Guid objectId, Dictionary<string, object>? parameters, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            logger.LogInformation("Executing command {CommandId} for object {ObjectId}", command.Id, objectId);

            var data = await ExecuteQueriesAsync(command, objectId, parameters, cancellationToken);
            var content = GenerateFileContent(command, data);
            var fileName = GenerateFileName(command, data, parameters);
            var filePath = await SaveContentToFileAsync(content, fileName, cancellationToken);

            try
            {
                await fileDeliveryService.DeliverFileAsync(filePath, fileName, command.Destination, cancellationToken);
                logger.LogInformation("Successfully executed command {CommandId}", command.Id);
            }
            finally
            {
                CleanupTempFile(filePath);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute command {CommandId} for object {ObjectId}", command.Id, objectId);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<Dictionary<string, object>> ExecuteQueriesAsync(ExternalCommand command, Guid objectId, Dictionary<string, object>? parameters, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, object>();

        foreach (var query in command.Queries)
        {
            var queryParameters = new Dictionary<string, object>
            {
                ["ObjectId"] = objectId,
            };

            parameters ??= new Dictionary<string, object>();
            foreach (var param in parameters)
            {
                queryParameters[param.Key] = param.Value;
            }

            var queryResult = await ExecuteSingleQueryAsync(query, queryParameters, cancellationToken);
            result[query.Name] = queryResult;
        }

        return result;
    }

    private async Task<object> ExecuteSingleQueryAsync(CommandQuery query, Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("DefaultConnection connection string not found in configuration");
        }

        await using var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = query.Query;
            command.CommandTimeout = settings.ExternalCommands.GlobalSettings.CommandTimeout;

            // Add parameters
            foreach (var param in parameters)
            {
                var sqlParam = new SqlParameter($"@{param.Key}", param.Value ?? DBNull.Value);
                command.Parameters.Add(sqlParam);
            }

            if (query.ResultType == CommandQueryResultType.Single)
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    return ReadRowToDictionary(reader);
                }

                return new Dictionary<string, object>();
            }
            else
            {
                var results = new List<Dictionary<string, object>>();
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    results.Add(ReadRowToDictionary(reader));
                }

                return results;
            }
        }
        finally
        {
            if (connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static Dictionary<string, object> ReadRowToDictionary(IDataReader reader)
    {
        var row = new Dictionary<string, object>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var value = reader.GetValue(i);
            row[reader.GetName(i)] = value == DBNull.Value ? null : value;
        }

        return row;
    }

    private string GenerateFileContent(ExternalCommand command, Dictionary<string, object> data)
    {
        return command.FileFormat switch
        {
            CommandFileFormat.XML => GenerateXmlContent(data),
            CommandFileFormat.JSON => GenerateJsonContent(data),
            _ => throw new NotSupportedException($"File format {command.FileFormat} is not supported")
        };
    }

    private string GenerateXmlContent(Dictionary<string, object> data)
    {
        var xmlSettings = settings.ExternalCommands.GlobalSettings.XmlSettings;
        var xmlDoc = new XmlDocument();

        if (xmlSettings.IncludeXmlDeclaration)
        {
            var declaration = xmlDoc.CreateXmlDeclaration("1.0", settings.ExternalCommands.GlobalSettings.FileEncoding, null);
            xmlDoc.AppendChild(declaration);
        }

        var rootElement = xmlDoc.CreateElement(xmlSettings.RootElementName);
        xmlDoc.AppendChild(rootElement);

        foreach (var kvp in data)
        {
            var element = xmlDoc.CreateElement(kvp.Key);

            if (kvp.Value is List<Dictionary<string, object>> list)
            {
                foreach (var item in list)
                {
                    var itemElement = xmlDoc.CreateElement("Item");
                    foreach (var itemKvp in item)
                    {
                        var itemProp = xmlDoc.CreateElement(itemKvp.Key);
                        itemProp.InnerText = itemKvp.Value?.ToString() ?? "";
                        itemElement.AppendChild(itemProp);
                    }

                    element.AppendChild(itemElement);
                }
            }
            else if (kvp.Value is Dictionary<string, object> dict)
            {
                foreach (var dictKvp in dict)
                {
                    var dictElement = xmlDoc.CreateElement(dictKvp.Key);
                    dictElement.InnerText = dictKvp.Value?.ToString() ?? "";
                    element.AppendChild(dictElement);
                }
            }
            else
            {
                element.InnerText = kvp.Value?.ToString() ?? "";
            }

            rootElement.AppendChild(element);
        }

        using var stringWriter = new StringWriter();
        using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
               {
                   Indent = xmlSettings.IndentXml,
                   Encoding = Encoding.GetEncoding(settings.ExternalCommands.GlobalSettings.FileEncoding)
               }))
        {
            xmlDoc.WriteTo(xmlWriter);
            xmlWriter.Flush();
        }

        return stringWriter.ToString();
    }

    private string GenerateJsonContent(Dictionary<string, object> data)
    {
        var jsonSettings = settings.ExternalCommands.GlobalSettings.JsonSettings;
        var options = new JsonSerializerOptions
        {
            WriteIndented = jsonSettings.IndentJson,
            PropertyNamingPolicy = jsonSettings.CamelCasePropertyNames ? JsonNamingPolicy.CamelCase : null
        };

        return JsonSerializer.Serialize(data, options);
    }

    private string GenerateFileName(ExternalCommand command, Dictionary<string, object> data, Dictionary<string, object>? parameters)
    {
        var fileName = command.FileNamePattern;

        // Replace common placeholders
        fileName = fileName.Replace("{Timestamp:yyyyMMddHHmmss}", DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
        fileName = fileName.Replace("{Timestamp}", DateTime.UtcNow.ToString("yyyyMMddHHmmssffff"));

        // Replace data placeholders
        foreach (var kvp in data)
        {
            if (kvp.Value is Dictionary<string, object> dict)
            {
                foreach (var dictKvp in dict)
                {
                    fileName = fileName.Replace($"{{{dictKvp.Key}}}", dictKvp.Value?.ToString() ?? "");
                }
            }
        }

        // Replace parameter placeholders
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                fileName = fileName.Replace($"{{{param.Key}}}", param.Value?.ToString() ?? "");
            }
        }

        return fileName;
    }

    private async Task<string> SaveContentToFileAsync(string content, string fileName, CancellationToken cancellationToken)
    {
        var tempPath = Path.GetTempPath();
        var filePath = Path.Combine(tempPath, fileName);

        await File.WriteAllTextAsync(filePath, content, Encoding.GetEncoding(settings.ExternalCommands.GlobalSettings.FileEncoding), cancellationToken);

        return filePath;
    }

    private void CleanupTempFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cleanup temporary file {FilePath}", filePath);
        }
    }
}