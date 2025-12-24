# AmazData

## AmazData.Module.PlcStat API 接口说明

本模块提供用于查询 PLC 统计数据的 RESTful API 接口。

### 1. 获取记录总数

获取数据库中 PLC 数据的总记录数。

- **接口地址**: `/api/plcstat/count`
- **请求方式**: `GET`
- **请求参数**: 无

**响应示例**:

```json
{
  "count": 123456
}
```

### 2. 获取传感器趋势数据

根据设备ID、传感器名称、时间范围和粒度查询趋势数据。

- **接口地址**: `/api/plcstat/trend`
- **请求方式**: `GET`

**请求参数 (Query Parameters)**:

| 参数名 | 类型 | 必填 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- | :--- |
| `DeviceId` | string | 是 | - | 设备 ID (例如: `ZTData`) |
| `SensorName` | string | 是 | - | 传感器名称 (例如: `FT_YBB0002_L`) |
| `Days` | int | 否 | `7` | 回溯天数 (必须大于 0) |
| `Granularity` | string | 否 | `Day` | 数据粒度。可选值: `Hour` (小时), `Day` (天) |

**返回格式**:

返回一个 JSON 数组，包含时间点和对应的平均值。
- 当粒度为 `Day` 时，`time` 格式为 `YYYY-MM-DD`。
- 当粒度为 `Hour` 时，`time` 格式为 `YYYY-MM-DD HH:mm:ss`。

**响应示例**:

```json
[
  {
    "time": "2025-12-21",
    "value": 5739.96
  },
  {
    "time": "2025-12-22",
    "value": 5855.97
  }
]
```

**调用示例 URL**:

1. **默认查询 (最近 7 天，按天汇总)**:
   ```
   GET /api/plcstat/trend?DeviceId=ZTData&SensorName=FT_YBB0002_L
   ```

2. **按小时查询最近 3 天的数据**:
   ```
   GET /api/plcstat/trend?DeviceId=ZTData&SensorName=FT_YBB0002_L&Days=3&Granularity=Hour
   ```

---

## AmazData.Module.Yunmou API 接口说明

本模块提供基于海康云眸 API 的视频流集成服务。

### 前置配置

在使用此 API 之前，请确保已在 Orchard Core 后台管理界面中：
1. 创建或编辑类型为 `YuMouKeyManage` 的内容项。
2. 配置正确的 `Client ID` 和 `Client Secret`。

### 1. 获取视频直播地址

根据设备序列号和通道号获取 HLS 协议的直播流地址。

- **接口地址**: `/api/yunmou/video`
- **请求方式**: `GET`

**请求参数 (Query Parameters)**:

| 参数名 | 类型 | 必填 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- | :--- |
| `deviceSerial` | string | 是 | - | 设备序列号 (例如: `D12345678`) |
| `channelNo` | int | 否 | `1` | 通道号，默认为 1 |

**响应格式**:

- **成功 (200 OK)**: 返回包含 `url` 的 JSON 对象。
- **失败**: 返回包含 `error`, `upstreamCode`, `message` 的 JSON 对象。

**响应示例 (成功)**:

```json
{
  "url": "https://hls01open.ys7.com/openlive/f01018a141094b7fa138b9d0b856507b.m3u8"
}
```

**响应示例 (失败)**:

```json
{
  "error": "Failed to get live address",
  "upstreamCode": 20002,
  "message": "Device not found"
}
```

**调用示例 URL**:

```
GET /api/yunmou/video?deviceSerial=C98765432&channelNo=1
```
