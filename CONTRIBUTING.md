# 如何为项目做贡献

## 开始之前

- 查看 [Issues](https://github.com/PCL-Community/PCL2-CE/issues) 寻找可以参与的任务，或创建新 Issue 讨论您的想法。

## 贡献流程

### 报告问题

1. 在提交 Issue 前，请先搜索是否已有相关 Issue
2. 使用提供的 Issue 模板
3. 包含以下信息：
   - 清晰的问题描述
   - 复现步骤（包括环境信息）
   - 预期与实际行为对比
   - 相关日志/截图（如有）

### 提交代码

1. Fork 仓库并克隆到本地

   ```bash
   git clone https://github.com/你的用户名/项目名称.git
   ```

2. 创建你的分支

   ```bash
   git checkout -b feat/your-feat-name
   # 或
   git checkout -b fix/issue-number-desc
   ```

3. 遵循项目代码风格进行编写
4. 提交更改，使用 Angular 规范提交信息

   ```bash
   git commit -m "<type>(scope): <subject>"
   ```

5. 推送分支到你的 Fork

   ```bash
   git push origin your-branch
   ```

6. 创建 Pull Request
   - 指向上游仓库的 `dev` 分支
   - 详细填写 PR 信息
   - 关联 Issue（如有）

## 开发规范

### 测试要求

- 提交前请在本地编译通过确保无误后提交

### Angular 规范

基本格式如下

```commit message
<type>(scope?): <subject>

<body>

<footer>
```

每次提交**必须包含页眉内容**，可以选用正文（`body`）和页脚（`footer`）

每次提交的信息不超过 `100` 个字符

#### 页眉（`header`）

页眉需包含提交类型（`type`）、作用域（`scope`，可选）和主题（`subject`）

##### 提交类型（`type`）

提交类型需指定为下面其中一个：

1. `build`：对构建系统或者外部依赖项进行修改
2. `chore`: 用于对非业务性代码进行修改，例如修改构建流程或者工具配置等
3. `ci`：对 CI 配置文件或脚本进行修改
4. `docs`：对文档进行修改
5. `feat`：增加新的特性
6. `fix`：修复 bug
7. `pref`：提高性能的代码更改
8. `refactor`：既不修复 bug 也不是添加特性的代码重构
9. `style`：不影响代码含义的修改，比如空格、格式化、缺失的分号等
10. `test`：增加缺失的测试或者修正已存在的测试

##### 作用域（`scope`）

范围可以是任何指定提交更改位置的内容

##### 主题（`subject`）

主题包括了对本次修改的简洁描述，有以下准则

1. 使用命令式与现在时态：`改变` 而不是 `已改变`，也不是 `改变了`
2. 不要大写首字母（若使用英文）
3. 不要在末尾添加句号

#### 正文（`body`）

同主题，使用命令式与现在时态

应包含修改的动机以及和之前行为的对比

#### 页脚（`footer`）

##### Breaking Changes

破坏性修改指的是本次提交使用了不兼容之前版本的 API 或者环境变量

所有不兼容修改都必须在页脚中作为破坏性修改提到，以 `BREAKING CHANGE:` 开头，后跟一个空格或者换行符，其余的信息就是对此次修改的描述、理由和注释

##### 引用完成的 Issue

如果本次提交目的是完成 Issue 的话，需在页脚引用该 Issue

以关键字 `Closes` 开头，如

```footer
Closes #1145
```

修改了多个 bug 以半角逗号和空格隔开

```footer
Closes #114, #514, #1919
```

#### 回滚（`revert`）

若此次提交包含回滚（`revert`）操作，那么页眉需以 `revert:` 开头，同时在正文中添加 `本次提交回滚到 commit <hash>`，其中 `<hash>` 值表示被回滚前的提交

```commit message
revert:<type>(<scope>): <subject>

本次提交回滚到 commit <hash>
<body>

<footer>
```
