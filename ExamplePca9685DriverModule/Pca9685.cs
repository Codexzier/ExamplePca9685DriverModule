/// #########################################################################################################
///
///  Blog: Meine Welt in meinem Kopf
///  Post: PCA9685 PWM Driver Module mit Rasperry Pi & Win 10 Iot
///  Postdate: 13.10.2018
///  --------------------------------------------------------------------------------------------------------
///  Kurze Information:
///  Diese Solution dient als Quellcode Beispiel und zeigt lediglich 
///  die Funktionsweise für das Initialisieren des Sensors und abruf der Daten.
///  Fehlerbehandlung, sowie Logging oder andere Erweiterungen 
///  für eine stabile Laufzeit der Anwendung sind nicht vorhanden.
///  
///  Für Änderungsvorschläge oder ergänzende Informationen zu meiner
///  Beispiel Anwendung, der oder die kann mich unter der Mail Adresse 
///  j.langner@gmx.net erreichen.
///  
///  Referenzen:
///  https://github.com/adafruit/Adafruit-PWM-Servo-Driver-Library/blob/master/Adafruit_PWMServoDriver.cpp
///  https://cdn-shop.adafruit.com/datasheets/PCA9685.pdf
///  
///  Vorraussetzung:
///  Raspberry Pi 2 oder 3
///  Windows 10 IoT
///  PCA9685 16 channel pwm driver
/// 
/// #########################################################################################################

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace ExamplePca9685DriverModule
{
    internal class Pca9685
    {
        private readonly byte _address = 0x40;

        private readonly byte PCA9685_MODE1 = 0x00;
        private readonly byte PCA9685_PRESCALE = 0xFE;

        private readonly byte LED0_ON_L = 0x06;
        private readonly byte LED0_ON_H = 0x07;

        private readonly byte LED0_OFF_L = 0x08;
        private readonly byte LED0_OFF_H = 0x09;

        private int _period;

        /// <summary>
        /// Klasse für die I²C Verbindung
        /// </summary>
        private I2cDevice _i2cDevice;

        /// <summary>
        /// Default I²C configuration are set address 0x40 and clock 100kHz
        /// </summary>
        /// <param name="period">Period (ms)</param>
        public Pca9685(int period) : base() => this._period = period * 1000;

        private async Task Init()
        {
            var i2cSettting = new I2cConnectionSettings(this._address);
            i2cSettting.BusSpeed = I2cBusSpeed.StandardMode;

            var deviceSelector = I2cDevice.GetDeviceSelector();

            var deviceInfo = await DeviceInformation.FindAllAsync(deviceSelector);
            this._i2cDevice = await I2cDevice.FromIdAsync(deviceInfo[0].Id, i2cSettting);

            if (this._i2cDevice == null)
            {
                throw new Exception("i2cDevice is null");
            }
        }

        /// <summary>
        /// Return to default mode
        /// </summary>
        public async Task Reset()
        {
            if(!this.Write(new byte[] { this.PCA9685_MODE1, 0x00 }))
            {
                throw new Exception("Can not send the reset command.");
            }

            await Task.Delay(10);
        }

        /// <summary>
        /// Set default and set frequence.
        /// </summary>
        internal async Task Start()
        {
            await this.Init();

            int hz = 1000000 / this._period;

            await this.Reset();
            await this.SetPwmFrequency(hz);
        }

        /// <summary>
        /// Set a new frequency.
        /// </summary>
        /// <param name="frequency"></param>
        public async Task SetPwmFrequency(float frequency)
        {
            byte prescale = this.GetPrescale(frequency);

            byte[] buffer = new byte[1] { this.PCA9685_MODE1 };

            if (!this.Read(buffer))
            {
                throw new Exception("can not read mode1");
            }

            byte oldMode = buffer[0];

            // sleep
            byte newMode = (byte)(((byte)oldMode & (byte)0x7F) | (byte)0x10);

            // go to sleep
            this.Write(new byte[] { this.PCA9685_MODE1, newMode });
            this.Write(new byte[] { this.PCA9685_PRESCALE, prescale });
            this.Write(new byte[] { this.PCA9685_MODE1, oldMode });
            await Task.Delay(5);
            this.Write(new byte[] { this.PCA9685_MODE1, (byte)(oldMode | 0x80) });

            byte[] bufferRead = new byte[] { this.PCA9685_MODE1 };
            this.Read(bufferRead);
            Debug.WriteLine("Mode now: " + bufferRead[0].ToString());
        }
        
        /// <summary>
        /// Set new puls.
        /// </summary>
        /// <param name="outputNumber">Set the number output.</param>
        /// <param name="on">set high puls.</param>
        /// <param name="off">set low puls.</param>
        internal void SetPwm(byte outputNumber, int on, int off)
        {
            Debug.WriteLine("Output: " + outputNumber.ToString() + ", ON: " + on.ToString() + ", OFF: " + off.ToString());

            byte targetOutput_ON_L = (byte)(this.LED0_ON_L + 4 * outputNumber);
            byte targetOutput_ON_H = (byte)(this.LED0_ON_H + 4 * outputNumber);
            byte targetOutput_OFF_L = (byte)(this.LED0_OFF_L + 4 * outputNumber);
            byte targetOutput_OFF_H = (byte)(this.LED0_OFF_H + 4 * outputNumber);

            this.Write(new byte[] { targetOutput_ON_L, (byte)on });
            this.Write(new byte[] { targetOutput_ON_H, (byte)(on >> 8) });
            this.Write(new byte[] { targetOutput_OFF_L, (byte)(off) });
            this.Write(new byte[] { targetOutput_OFF_H, (byte)(off >> 8) });
        }

        /// <summary>
        /// Create the prescale value for the module to use the target frequency.
        /// </summary>
        /// <param name="frequency"></param>
        /// <returns></returns>
        private byte GetPrescale(float frequency)
        {
            frequency *= 0.9f;
            // internal clock frequency
            float prescaleval = 25000000;
            prescaleval /= 4096;
            prescaleval /= frequency;
            prescaleval -= 1;

            return (byte)(prescaleval + 0.5);
        }

        /// <summary>
        /// Sendet das Byte Array zum Modul
        /// </summary>
        /// <param name="buffer">Übergabe des Byte Arrays mit der Inhaltlichen Aufgabe.</param>
        /// <returns>Gibt einen Wert zurück, ob das Schreiben erfolgreich war oder nicht.</returns>
        private bool Write(byte[] buffer)
        {
            var result = this._i2cDevice.WritePartial(buffer);

            if (result.Status != I2cTransferStatus.FullTransfer)
            {
                Debug.WriteLine(result.Status);
            }

            return result.Status == I2cTransferStatus.FullTransfer;
        }

        /// <summary>
        /// Liest mit den Byte Array die Daten vom Modul
        /// </summary>
        /// <param name="buffer">Der zu beschreibende Buffer</param>
        /// <returns>Gibt einen Wert zurück, ob das Einlesen erfolgreich war oder nicht.</returns>
        private bool Read(byte[] buffer)
        {
            var result = this._i2cDevice.ReadPartial(buffer);

            if (result.Status != I2cTransferStatus.FullTransfer)
            {
                Debug.WriteLine(result.Status);
            }

            return result.Status == I2cTransferStatus.FullTransfer;
        }
    }
}
