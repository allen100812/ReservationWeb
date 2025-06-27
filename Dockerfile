# -------- 第一階段：建置專案（Build） --------
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# 複製所有檔案進入容器中的 /src 資料夾
COPY . .

# 執行 dotnet publish，編譯成 Release 模式並輸出到 /app/publish
RUN dotnet publish -c Release -o /app/publish


# -------- 第二階段：執行環境（Runtime） --------
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app

# 將上一步 build 出來的檔案複製到執行容器中
COPY --from=build /app/publish .

# 開放 port 80 給外部使用
EXPOSE 80

# 啟動應用程式
ENTRYPOINT ["dotnet", "Web0524.dll"]
