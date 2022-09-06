using BlazorWebApp.Models;
using Microsoft.JSInterop;

namespace BlazorWebApp.Shared
{
    /// <summary>
    /// คลาสสำหรับเรียกใช้ Firebase Realtime Database จาก C# ไปยัง Firebase JavaScript SDK
    /// โดยใช้ JSInterop
    /// 
    /// คลาสนี้ออกแบบมาให้ทำงานเป็น service โดยจะต้องเพิ่มคำสั่ง
    /// <code>builder.Services.AddScoped<Firebase>();</code>
    /// ไว้ในไฟล์ Program.cs เพื่อสร้าง instance ของคลาสไว้ และให้ page อื่นๆเรียกใช้งานได้
    /// 
    /// การเรียกใช้จาก page อื่น ๆ ทำได้โดยใช้ dependency injection โดยจะต้องเพิ่มคำสั่ง
    /// <code>@inject Firebase Firebase</code>
    /// ไว้ในหน้า page จึงจะใช้งานตัวแปร Firebase ได้
    /// </summary>
    public class Firebase : IDisposable
    {
        private ILogger<Firebase> Logger;
        private readonly IJSRuntime js;
        private DotNetObjectReference<Firebase>? objRef;

        public bool IsDataConnected { get; private set; }
        public Firebase(IJSRuntime js, ILogger<Firebase> logger)
        {
            this.js = js;
            objRef = DotNetObjectReference.Create(this);
            AttachOnValue<Firebase, bool>(".info/connected", objRef, UpdateDataConnection);
            // Initialize Data ควรจะสร้างเป็น Dictionary เพราะ key จะถูกใช้เป็นชื่อ path เพื่ออ้างอิงข้อมูลนั้น
            // หมายเหตุ: ค่า key จะต้องไม่เป็นตัวเลขล้วน เพราะ Firebase จะเข้าใจผิดว่าเป็น List
            EnsureCreated(new Dictionary<string, object> {
                { "weathers",
                    new Dictionary<string, WeatherForecast>() {
                        { "ID01", new WeatherForecast {
                            Date = new DateTime(2018,05,06),
                            TemperatureC = 1,
                            Summary = "Freezing" }
                        },
                        { "ID02", new WeatherForecast {
                            Date = new DateTime(2018,05,07),
                            TemperatureC = 14,
                            Summary = "Bracing" }
                        },
                        { "ID03", new WeatherForecast {
                            Date = new DateTime(2018,05,08),
                            TemperatureC = -13,
                            Summary = "Freezing" }
                        },
                        { "ID04", new WeatherForecast {
                            Date = new DateTime (2018,05,09),
                            TemperatureC = -16,
                            Summary = "Balmy" }
                        },
                        { "ID05", new WeatherForecast {
                            Date= new DateTime(2018,05,10),
                            TemperatureC = -2,
                            Summary = "Chilly" }
                        }
                    }
                },
                { "users",
                    new Dictionary<string, string>()
                    {
                        { "somchai", "somchai.l@psu.ac.th" }
                    }
                }
            });
            Logger = logger;
        }

        public void Dispose()
        {
            if (objRef is not null)
            {
                DetachOnValue<Firebase>(".info/connected", objRef);
                objRef = null;
            }
            GC.SuppressFinalize(this);
        }

        [JSInvokable]
        public void UpdateDataConnection(bool newValue)
        {
            IsDataConnected = newValue;
            if (IsDataConnected)
                Logger.LogInformation("Database is connected.");
            else
                Logger.LogInformation("Database is not connected.");
        }

        /// <summary>
        /// ตรวจสอบว่า Table ข้อมูลมีอยู่หรือไม่ หากไม่มี ก็จะสร้างขึ้นอัตโนมัติตามข้อมูลใน initData
        /// </summary>
        /// <param name="initData">ข้อมูลเริ่มต้น</param>
        public async void EnsureCreated(Dictionary<string, object> initData)
        {
            await js.InvokeVoidAsync("EnsureCreated", initData);
        }
        /// <summary>
        /// สร้างข้อมูลใหม่เป็นลูกของ parentPath โดย generate key ที่ไม่ซ้ำมา และ return ค่า key
        /// </summary>
        /// <typeparam name="TValue">ชนิดของข้อมูล</typeparam>
        /// <param name="parentPath">path ของ parent</param>
        /// <param name="initValue">ค่าเริ่มต้นของข้อมูลใหม่</param>
        /// <returns>ค่า key ของข้อมูล, null หากสร้างไม่สำเร็จ</returns>
        public async Task<string> Push<TValue>(string parentPath, TValue? initValue)
        {
            return await js.InvokeAsync<string>("PushData", parentPath, initValue);
        }

        /// <summary>
        /// อ่านข้อมูลที่ datapath
        /// </summary>
        /// <typeparam name="TValue">ชนิดของข้อมูล</typeparam>
        /// <param name="datapath">path ของข้อมูล</param>
        /// <returns></returns>
        public async Task<TValue> Get<TValue>(string datapath)
        {
            return await js.InvokeAsync<TValue>("GetData", datapath);
        }

        /// <summary>
        /// บันทึกข้อมูลที่ datapath
        /// </summary>
        /// <typeparam name="TValue">ชนิดของข้อมูล</typeparam>
        /// <param name="datapath">path ของข้อมูล</param>
        /// <param name="value">ข้อมูลที่ต้องการบันทึก</param>
        public async void Set<TValue>(string datapath, TValue value)
        {
            await js.InvokeVoidAsync("SetData", datapath, value);
        }

        /// <summary>
        /// ลบข้อมูลที่ datapath
        /// </summary>
        /// <param name="datapath">path ของข้อมูล</param>
        /// 
        public async void Remove(string datapath)
        {
            await js.InvokeVoidAsync("RemoveData", datapath);
        }

        /// <summary>
        /// ประกาศรูปแบบของ OnValue callback ฟังก์ชัน ที่จะถูกเรียกเมื่อเกิด event onValue ขึ้น
        /// </summary>
        /// <typeparam name="TValue">ชนิดของข้อมูล</typeparam>
        /// <param name="value">ค่าของข้อมูล</param>
        public delegate void OnValueCallBack<TValue>(TValue value);

        /// <summary>
        /// ผูก objRef.callback() ฟังก์ชันไว้กับ event onValue ของ datapath
        /// เมื่อข้อมูลที่ datapath เปลี่ยนแปลง ก็จะเรียกไปยัง objRef.callback(snapshot.val())
        /// 
        /// เมื่อผูกฟังก์ชันไว้แล้ว ควรจะถอดออกด้วยคำสั่ง DetachOnValue() เมื่อไม่ใช้งาน โดยปกติแล้ว จะเรียกใช้
        /// ตอนที่ Dispose()
        /// </summary>
        /// <typeparam name="T">ชนิดของคลาสที่แปลงเป็น DotNetObjectReference แล้ว</typeparam>
        /// <typeparam name="TValue">ชนิดของข้อมูลที่ datapath อ้างถึง</typeparam>
        /// <param name="dataPath">path ของข้อมูล</param>
        /// <param name="objRef">DotNetObjectReference ที่ต้องการผูกไว้</param>
        /// <param name="callback">OnValueCallBack ฟังก์ชันที่ต้องการผูกไว้</param>
        public async void AttachOnValue<T, TValue>(string dataPath, DotNetObjectReference<T> objRef, OnValueCallBack<TValue> callback) where T : class
        {
            await js.InvokeVoidAsync("AttachOnValue", dataPath, objRef, callback.Method.Name);
        }

        /// <summary>
        /// ถอด objRef.callback() ฟังก์ชันที่ผูกไว้กับ event onValue ของ datapath ออก
        /// </summary>
        /// <typeparam name="T">ชนิดของคลาสที่แปลงเป็น DotNetObjectReference แล้ว</typeparam>
        /// <param name="dataPath">path ของข้อมูล</param>
        /// <param name="objRef">DotNetObjectReference ที่ต้องการถอด</param>
        public async void DetachOnValue<T>(string dataPath, DotNetObjectReference<T> objRef) where T : class
        {
            await js.InvokeVoidAsync("DetachOnValue", dataPath, objRef);
        }


    }
}
