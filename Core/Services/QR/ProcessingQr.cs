// Core/Services/QR/ProcessingQr.cs
using QRCoder;
using System.Text;

namespace ease_intro_api.Core.Services.QR;

public class ProcessingQr
{

    private static string CreateQrSource(Guid meetUid)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return $"{timestamp}-{meetUid}";
    }

    /**
     * Создает исходную строку для QR-кода на основании текущей даты и <b>Guid</b> встречи
     *
     * <param name="meetUid">Уникальный идентификатор встречи.</param>
     * <returns>Строка-идентификатор на основании которой генерируется изображение QR кода.</returns>
     */
    public static string GenerateQr(Guid meetUid)
    {
        var source = CreateQrSource(meetUid);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(source));
    }

    /**
     * Возвращает QR-код в виде PNG изображения
     *
     * <param name="data">Данные которые будут кодироваться в QR коде.</param>
     * <returns>Изображение QR кода для сканирокания</returns>
     */
    public static byte[] GenerateQrPng(string data)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        var pngQrCode = new PngByteQRCode(qrCodeData);
        byte[] pngBytes = pngQrCode.GetGraphic(42);
        return pngBytes;
    }
}