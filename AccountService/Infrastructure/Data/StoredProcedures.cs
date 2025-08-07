namespace AccountService.Infrastructure.Data;

public static class StoredProcedures
{
    public static string GetAccrueInterestProcedureScript()
    {
        return """
               CREATE OR REPLACE FUNCTION accrue_interest(account_id UUID)
               RETURNS VOID AS $$
               DECLARE
                   account_record RECORD;
                   interest_amount DECIMAL(18,2);
                   days_passed INTEGER;
               BEGIN
                   -- Получаем информацию о счёте
                   SELECT * INTO account_record 
                   FROM "Accounts" 
                   WHERE "Id" = account_id 
                     AND "Type" = 1 -- Deposit account
                     AND "IsDeleted" = false;

                   IF NOT FOUND THEN
                       RAISE EXCEPTION 'Депозитный счёт с ID % не найден', account_id;
                   END IF;

                   -- Проверка ставки
                   IF account_record."InterestRate" IS NULL OR account_record."InterestRate" <= 0 THEN
                       RAISE EXCEPTION 'У счёта % отсутствует или некорректная процентная ставка', account_id;
                   END IF;

                   -- Количество дней
                   days_passed := (NOW()::DATE - account_record."CreatedAt"::DATE);

                   IF days_passed <= 0 THEN
                       RETURN;
                   END IF;

                   -- Расчёт процентов
                   interest_amount := account_record."Balance" * (account_record."InterestRate" / 100) * (days_passed / 365.0);

                   IF interest_amount > 0 THEN
                       -- Обновление баланса
                       UPDATE "Accounts" 
                       SET "Balance" = "Balance" + interest_amount,
                           "UpdatedAt" = NOW()
                       WHERE "Id" = account_id;

                       -- Создание транзакции
                       INSERT INTO "Transactions" (
                           "Id", "AccountId", "Amount", "Currency", "Type", 
                           "Description", "Date", "IsCanceled", "CanceledAt"
                       ) VALUES (
                           gen_random_uuid(),
                           account_id,
                           interest_amount,
                           account_record."Currency",
                           0, -- Credit
                           'Начисление процентов по депозиту',
                           NOW(),
                           false,
                           NULL
                       );
                   END IF;
               END;
               $$ LANGUAGE plpgsql;
               """;
    }
}