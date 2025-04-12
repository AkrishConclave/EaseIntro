using QRCoder;
using System.Text;

namespace ease_intro_api.Core.Services;

public class ProcessingQr
{

    /// <summary>
    /// Формирует строку состоящую из текущей даты и идентификатора встречи.
    /// </summary>
    /// <param name="meetUid">Идентификатор встречи.</param>
    /// <returns>Необходимая строка.</returns>
    private static string CreateQrSource(Guid meetUid)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return $"{timestamp}-{meetUid}";
    }
    
    /// <summary>
    /// Создает исходную строку для QR-кода на основании текущей даты и <b>Guid</b> встречи
    /// </summary>
    /// <param name="meetUid">Уникальный идентификатор встречи.</param>
    /// <returns>Строка-идентификатор на основании которой генерируется изображение QR-кода.</returns>
    public static string GenerateQr(Guid meetUid)
    {
        var source = CreateQrSource(meetUid);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(source));
    }
    
    /// <summary>
    /// Возвращает QR-код в виде PNG изображения.
    /// </summary>
    /// <param name="data">Данные, которые будут закодированы в QR-код.</param>
    /// <returns>Изображение QR-кода для сканирования.</returns>
    public static byte[] GenerateQrPng(string data)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        var pngQrCode = new PngByteQRCode(qrCodeData);
        byte[] pngBytes = pngQrCode.GetGraphic(42);
        return pngBytes;
    }
}