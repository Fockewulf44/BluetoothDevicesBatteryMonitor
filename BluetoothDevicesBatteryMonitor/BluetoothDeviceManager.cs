using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace BluetoothDevicesBatteryMonitor
{
    public class BluetoothDeviceManager
    {


        public event EventHandler<BluetoothDeviceInfo> BluetoothDeviceModified;


        public void GetBluetoothDevices()
        {
            string deviceSelector = BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected);

            DeviceWatcher dWatcher = DeviceInformation.CreateWatcher(deviceSelector);

            dWatcher.Added += BluetoothAdded;
            dWatcher.Updated += BluetoothUpdated;

            dWatcher.Start();
        }

        private void BluetoothAdded(DeviceWatcher deviceWatcher, DeviceInformation deviceInformation)
        {
            ulong blAddress = GetBluetoothAddressFromId(deviceInformation.Id);

            BluetoothDeviceModified.Invoke(this, new BluetoothDeviceInfo
            {
                Name = deviceInformation.Name,
                Id = deviceInformation.Id,
                BatteryLevel = blAddress == 0 ? "0" : GetBluetoothBatteryLevelAsync(blAddress).Result.ToString()
            });
        }


        private void BluetoothUpdated(DeviceWatcher deviceWatcher, DeviceInformationUpdate deviceInformation)
        {
            BluetoothDeviceModified.Invoke(this, new BluetoothDeviceInfo
            {
                Name = deviceInformation.Properties.ToArray().ToString(),
                Id = deviceInformation.Id
            });
        }

        protected ulong GetBluetoothAddressFromId(string id)
        {
            string blAddressSTR = id.Substring(id.LastIndexOf("-") + 1).Replace(":", "").Replace("-", "");
            ulong blAddress = 0;

            if (ulong.TryParse(blAddressSTR, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out ulong result))
            {
                blAddress = result;
            }

            return blAddress;
        }

        public async Task<string> GetBluetoothBatteryLevelAsync(ulong bluetoothAddress)
        {
            string batteryInfo = "";

            BluetoothLEDevice device = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);

            if (device == null)
            {
                return batteryInfo;
            }

            GattDeviceServicesResult serviceResult = await device.GetGattServicesForUuidAsync(GattServiceUuids.Battery);


            if (serviceResult.Status == GattCommunicationStatus.Success && serviceResult.Services.Count > 0)
            {
                foreach (GattDeviceService? sr in serviceResult.Services)
                {
                    GattCharacteristicsResult charResult = await sr.GetCharacteristicsForUuidAsync(GattCharacteristicUuids.BatteryLevel);

                    if (charResult.Status == GattCommunicationStatus.Success && charResult.Characteristics.Count > 0)
                    {
                        GattCharacteristic batteryCharacteristic = charResult.Characteristics[0];

                        GattReadResult readResult = await batteryCharacteristic.ReadValueAsync();

                        if (readResult.Status == GattCommunicationStatus.Success)
                        {
                            using (DataReader reader = DataReader.FromBuffer(readResult.Value))
                            {
                                byte batteryLevel = reader.ReadByte();
                                batteryInfo = batteryInfo == "" ? batteryLevel.ToString() : batteryInfo + ":" + batteryLevel.ToString();
                            }
                        }
                    }
                }
            }

            return batteryInfo;
        }
    }

    public struct BluetoothDeviceInfo
    {
        public string? Name { get; set; }
        public string Id { get; set; }
        public BluetoothDevice Device { get; set; }
        public string BatteryLevel { get; set; }

    }
}
