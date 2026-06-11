# Checklist

- [x] `.github/workflows/backend-quality-gate.yml` line 73 uses `ConnectionStrings__Default` instead of `WeCMS__ConnectionStrings__Default`
- [x] The connection string value uses `User=root;Password=wecms_dev_pass` matching the MySQL service container credentials
