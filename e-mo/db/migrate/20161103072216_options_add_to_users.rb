class OptionsAddToUsers < ActiveRecord::Migration[5.0]
  def change
    change_column_null :users, :login_id, false
    change_column_null :users, :password, false
    change_column_null :users, :connected_flag, false
    change_column_null :users, :teacher_flag, false
    change_column_null :users, :admin_flag, false

    change_column_default :users, :teacher_flag, false
    change_column_default :users, :connected_flag, false
    change_column_default :users, :admin_flag, false
  end
end
